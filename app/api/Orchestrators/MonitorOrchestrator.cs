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

        var newDogAids = diff.NewDogs.Select(d => d.Aid).ToHashSet();
        var existingDogs = currentDogs
            .Where(d => !newDogAids.Contains(d.Aid))
            .ToList();

        var dogsToStore = await HandleNewDogsAsync(currentDogs, diff.NewDogs, existingDogs, ct);

        if (diff.RemovedAids.Count > 0)
        {
            logger.LogInformation("{Count} dog(s) removed", diff.RemovedAids.Count);
            var adoptedDogs = await dogRepository.GetByAidsAsync(diff.RemovedAids, ct);
            await adoptedDogRepository.SaveAsync(adoptedDogs, DateTimeOffset.UtcNow, ct);
            await dogRepository.RemoveDogsAsync(diff.RemovedAids, ct);
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
            .Select(d => scrapingEngine.GetDogDetailAsync(d.Aid, ct))
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
            .ToDictionary(d => d.Aid);

        if (newDogs.Count > 0)
        {
            var subscriptions = await subscriptionRepository.GetAllAsync(ct);

            foreach (var dog in newDogs.Select(d => enrichedDogs.TryGetValue(d.Aid, out var e) ? e : d))
            {
                var payload = notificationEngine.BuildPayload(dog);
                var sendTasks = subscriptions
                    .Select(sub => SendAndCleanupAsync(payload, sub, ct))
                    .ToList();

                await Task.WhenAll(sendTasks);
            }
        }

        return allCurrentDogs
            .Select(dog => enrichedDogs.TryGetValue(dog.Aid, out var enriched) ? enriched : dog)
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
    }

    private async Task SaveCurrentStateAsync(IReadOnlyList<Dog> dogs, CancellationToken ct)
    {
        var aids = dogs.Select(d => d.Aid).ToList();
        var dogMap = dogs.ToDictionary(d => d.Aid, d => d.Name ?? "Unknown");
        var state = new SiteState(aids, dogMap, DateTimeOffset.UtcNow);

        await stateRepository.SaveStateAsync(state, ct);
    }
}
