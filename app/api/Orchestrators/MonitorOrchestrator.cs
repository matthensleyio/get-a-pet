using System.Net;

using Microsoft.Extensions.Logging;

using Api.DomainModels;
using Api.Engines;
using Api.Repositories;
using WebPush;

namespace Api.Orchestrators;

public sealed class MonitorOrchestrator(
    ScrapingEngine scrapingEngine,
    ShelterLuvEngine shelterLuvEngine,
    DogDiffEngine dogDiffEngine,
    NotificationEngine notificationEngine,
    StateRepository stateRepository,
    DogRepository dogRepository,
    SubscriptionRepository subscriptionRepository,
    ILogger<MonitorOrchestrator> logger)
{
    public async Task CheckAsync(CancellationToken ct)
    {
        var state = await stateRepository.GetStateAsync(ct);

        // Fetch all sources in parallel
        var khsTask          = scrapingEngine.GetAllDogsAsync(ct);
        var shelterLuvTask   = shelterLuvEngine.GetAllConfiguredSheltersAsync(ct);

        await Task.WhenAll(khsTask, shelterLuvTask);

        var currentDogs = (await khsTask)
            .Concat((await shelterLuvTask).SelectMany(r => r.Dogs))
            .ToList();

        if (state is null)
        {
            await HandleFirstRunAsync(currentDogs, ct);
            return;
        }

        var diff = dogDiffEngine.ComputeDiff(currentDogs, state);

        // Only KHS dogs need a separate detail fetch
        var aidsNeedingDetails = await dogRepository.GetAidsNeedingDetailsAsync(ct);
        var aidsNeedingDetailsSet = aidsNeedingDetails
            .Where(x => x.Shelter == "Kansas Humane Society")
            .Select(x => x.Aid)
            .ToHashSet();

        var newDogAids = diff.NewDogs.Select(DogDiffEngine.ShelterKey).ToHashSet();
        var backfillDogs = currentDogs
            .Where(d => d.Shelter == "Kansas Humane Society"
                     && !newDogAids.Contains(DogDiffEngine.ShelterKey(d))
                     && aidsNeedingDetailsSet.Contains(d.Aid))
            .ToList();

        var dogsToStore = (diff.NewDogs.Count > 0 || backfillDogs.Count > 0)
            ? await HandleNewDogsAsync(currentDogs, diff.NewDogs, backfillDogs, ct)
            : currentDogs;

        if (diff.RemovedDogs.Count > 0)
        {
            logger.LogInformation("{Count} dog(s) removed", diff.RemovedDogs.Count);
            await dogRepository.RemoveDogsAsync(diff.RemovedDogs, ct);
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

        if (backfillDogs.Count > 0)
        {
            logger.LogInformation("Backfilling details for {Count} KHS dog(s)", backfillDogs.Count);
        }

        // Only KHS dogs need a separate detail fetch; ShelterLuv dogs have full data already
        var khsDogsToFetch = newDogs
            .Where(d => d.Shelter == "Kansas Humane Society")
            .Concat(backfillDogs)
            .ToList();

        var enrichedDogs = new Dictionary<string, Dog>();

        if (khsDogsToFetch.Count > 0)
        {
            var detailTasks = khsDogsToFetch
                .Select(d => scrapingEngine.GetDogDetailAsync(d.Aid, ct))
                .ToList();

            var details = await Task.WhenAll(detailTasks);

            foreach (var (dog, i) in khsDogsToFetch.Select((d, i) => (d, i)))
            {
                var enriched = details[i] is { } detail ? dog with
                {
                    Breed           = detail.Breed,
                    Color           = detail.Color,
                    Size            = detail.Size,
                    Weight          = detail.Weight,
                    AdoptionFee     = detail.AdoptionFee,
                    CurrentLocation = detail.CurrentLocation
                } : dog;

                enrichedDogs[DogDiffEngine.ShelterKey(dog)] = enriched;
            }
        }

        if (newDogs.Count > 0)
        {
            var subscriptions = await subscriptionRepository.GetAllAsync(ct);

            foreach (var dog in newDogs.Select(d =>
                enrichedDogs.TryGetValue(DogDiffEngine.ShelterKey(d), out var e) ? e : d))
            {
                var payload = notificationEngine.BuildPayload(dog);
                var sendTasks = subscriptions
                    .Select(sub => SendAndCleanupAsync(payload, sub, ct))
                    .ToList();

                await Task.WhenAll(sendTasks);
            }
        }

        return allCurrentDogs
            .Select(dog => enrichedDogs.TryGetValue(DogDiffEngine.ShelterKey(dog), out var enriched) ? enriched : dog)
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
        var aids   = dogs.Select(DogDiffEngine.ShelterKey).ToList();
        var dogMap = dogs.ToDictionary(DogDiffEngine.ShelterKey, d => d.Name ?? "Unknown");
        var state  = new SiteState(aids, dogMap, DateTimeOffset.UtcNow);

        await stateRepository.SaveStateAsync(state, ct);
    }
}
