using System.Net;

using Microsoft.Extensions.Logging;

using Api.DomainModels;
using Api.Engines;
using Api.Repositories;
using WebPush;

namespace Api.Orchestrators;

public sealed class MonitorOrchestrator(
    ScrapingEngine scrapingEngine,
    DogDiffEngine dogDiffEngine,
    NotificationEngine notificationEngine,
    StateRepository stateRepository,
    DogRepository dogRepository,
    AdoptedDogRepository adoptedDogRepository,
    SubscriptionRepository subscriptionRepository,
    ILogger<MonitorOrchestrator> logger)
{
    public async Task CheckAsync(CancellationToken ct)
    {
        await adoptedDogRepository.PruneOldAsync(ct);

        var state = await stateRepository.GetStateAsync(ct);
        var currentDogs = await scrapingEngine.GetAllDogsAsync(ct);

        if (state is null)
        {
            await HandleFirstRunAsync(currentDogs, ct);
            return;
        }

        var diff = dogDiffEngine.ComputeDiff(currentDogs, state);

        var newDogKeys = diff.NewDogs.Select(DogDiffEngine.CompositeKey).ToHashSet();
        var existingDogs = currentDogs
            .Where(d => !newDogKeys.Contains(DogDiffEngine.CompositeKey(d)))
            .ToList();

        var dogsToStore = (await HandleNewDogsAsync(currentDogs, diff.NewDogs, existingDogs, ct)).ToList();

        if (diff.RemovedAids.Count > 0)
        {
            logger.LogInformation("{Count} dog(s) removed from listing, verifying…", diff.RemovedAids.Count);
            var removedDogs = await dogRepository.GetByKeysAsync(diff.RemovedAids, ct);

            var verifyTasks = removedDogs
                .Select(d => scrapingEngine.IsStillAvailableAsync(d.Aid, d.ShelterId, ct))
                .ToList();
            var stillAvailable = await Task.WhenAll(verifyTasks);

            var trulyAdopted = removedDogs.Where((_, i) => !stillAvailable[i]).ToList();
            var falselyRemoved = removedDogs.Where((_, i) => stillAvailable[i]).ToList();

            if (falselyRemoved.Count > 0)
            {
                logger.LogWarning(
                    "{Count} dog(s) still on PetBridge detail page — not marking adopted: {Names}",
                    falselyRemoved.Count,
                    string.Join(", ", falselyRemoved.Select(d => d.Name)));
                dogsToStore.AddRange(falselyRemoved);
            }

            if (trulyAdopted.Count > 0)
            {
                logger.LogInformation("{Count} dog(s) confirmed adopted", trulyAdopted.Count);
                var adoptedKeys = trulyAdopted.Select(DogDiffEngine.CompositeKey).ToList();
                await adoptedDogRepository.SaveAsync(trulyAdopted, DateTimeOffset.UtcNow, ct);
                await dogRepository.RemoveDogsAsync(adoptedKeys, ct);
            }
        }

        await dogRepository.UpsertDogsAsync(dogsToStore, ct);
        await SaveCurrentStateAsync(dogsToStore, ct);
    }

    private async Task HandleFirstRunAsync(IReadOnlyList<Dog> dogs, CancellationToken ct)
    {
        logger.LogInformation("First run, capturing initial state with {Count} dog(s)", dogs.Count);

        await dogRepository.UpsertDogsAsync(dogs, ct);
        await SaveCurrentStateAsync(dogs, ct);
    }

    private async Task<IReadOnlyList<Dog>> HandleNewDogsAsync(
        IReadOnlyList<Dog> allCurrentDogs,
        IReadOnlyList<Dog> newDogs,
        IReadOnlyList<Dog> backfillDogs,
        CancellationToken ct)
    {
        if (newDogs.Count > 0)
        {
            logger.LogInformation("Found {Count} new dog(s)", newDogs.Count);
        }

        var dogsToFetch = newDogs.Concat(backfillDogs).ToList();

        var detailTasks = dogsToFetch
            .Select(d => scrapingEngine.GetDogDetailAsync(d.Aid, d.ShelterId, ct))
            .ToList();

        var details = await Task.WhenAll(detailTasks);

        var enrichedDogs = dogsToFetch
            .Select((dog, i) => details[i] is { } detail ? dog with
            {
                Breed = detail.Breed,
                Color = detail.Color,
                Size = detail.Size,
                Weight = detail.Weight,
                AdoptionFee = detail.AdoptionFee,
                CurrentLocation = detail.CurrentLocation,
                IntakeDate = detail.IntakeDate ?? dog.IntakeDate
            } : dog)
            .ToDictionary(DogDiffEngine.CompositeKey);

        if (newDogs.Count > 0)
        {
            var subscriptions = await subscriptionRepository.GetAllAsync(ct);

            foreach (var dog in newDogs.Select(d => enrichedDogs.TryGetValue(DogDiffEngine.CompositeKey(d), out var e) ? e : d))
            {
                var payload = notificationEngine.BuildPayload(dog);
                var relevantSubs = subscriptions
                    .Where(s => s.ShelterIds.Count == 0 || s.ShelterIds.Contains(dog.ShelterId))
                    .ToList();
                var sendTasks = relevantSubs
                    .Select(sub => SendAndCleanupAsync(payload, sub, ct))
                    .ToList();

                await Task.WhenAll(sendTasks);
            }
        }

        return allCurrentDogs
            .Select(dog => enrichedDogs.TryGetValue(DogDiffEngine.CompositeKey(dog), out var enriched) ? enriched : dog)
            .ToList();
    }

    private async Task SendAndCleanupAsync(
        NotificationPayload payload,
        DomainModels.PushSubscription sub,
        CancellationToken ct)
    {
        try
        {
            await notificationEngine.SendAsync(payload, sub, ct);
        }
        catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
        {
            logger.LogWarning("Removing expired subscription: {Endpoint}", sub.Endpoint);
            await subscriptionRepository.RemoveByEndpointAsync(sub.Endpoint, ct);
        }
        catch (WebPushException ex)
        {
            logger.LogWarning("Transient push failure for {Endpoint}: {Message}", sub.Endpoint, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("Network error sending push to {Endpoint}: {Message}", sub.Endpoint, ex.Message);
        }
    }

    private async Task SaveCurrentStateAsync(IReadOnlyList<Dog> dogs, CancellationToken ct)
    {
        var keys = dogs.Select(DogDiffEngine.CompositeKey).ToList();
        var dogMap = dogs.ToDictionary(DogDiffEngine.CompositeKey, d => d.Name ?? "Unknown");
        var state = new SiteState(keys, dogMap, DateTimeOffset.UtcNow);

        await stateRepository.SaveStateAsync(state, ct);
    }
}
