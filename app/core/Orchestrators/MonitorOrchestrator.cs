using System.Net;

using Microsoft.Extensions.Logging;

using Core.DomainModels;
using Core.Engines;
using Core.Repositories;
using WebPush;

namespace Core.Orchestrators;

public sealed class MonitorOrchestrator(
    ScrapingEngine scrapingEngine,
    ShelterLuvScrapingEngine shelterLuvScrapingEngine,
    ShelterLuvV3ScrapingEngine shelterLuvV3ScrapingEngine,
    DogDiffEngine dogDiffEngine,
    NotificationEngine notificationEngine,
    StateRepository stateRepository,
    DogRepository dogRepository,
    AdoptedDogRepository adoptedDogRepository,
    SubscriptionRepository subscriptionRepository,
    IReadOnlyList<ShelterLuvConfig> shelterLuvConfigs,
    IReadOnlyList<ShelterLuvV3Config> shelterLuvV3Configs,
    ILogger<MonitorOrchestrator> logger)
{
    public async Task CheckAsync(CancellationToken ct)
    {
        await adoptedDogRepository.PruneOldAsync(ct);

        var state = await stateRepository.GetStateAsync(ct);
        var petBridgeDogs = await scrapingEngine.GetAllDogsAsync(ct);
        var shelterLuvDogs = await shelterLuvScrapingEngine.GetAllDogsAsync(ct);
        var shelterLuvV3Dogs = await shelterLuvV3ScrapingEngine.GetAllDogsAsync(ct);
        var currentDogs = petBridgeDogs
            .Concat(shelterLuvDogs)
            .Concat(shelterLuvV3Dogs)
            .DistinctBy(DogDiffEngine.CompositeKey)
            .ToList();

        if (state is null)
        {
            await HandleFirstRunAsync(currentDogs, ct);
            return;
        }

        var diff = dogDiffEngine.ComputeDiff(currentDogs, state);

        var firstTimeShelterIds = currentDogs
            .Select(d => d.ShelterId)
            .Distinct()
            .Where(id => !state.KnownAids.Any(key => key.StartsWith(id + "-", StringComparison.Ordinal)))
            .ToHashSet();

        if (firstTimeShelterIds.Count > 0)
        {
            logger.LogInformation(
                "Suppressing notifications for first-time shelter(s): {Shelters}",
                String.Join(", ", firstTimeShelterIds));
        }

        // Filter out dogs that were already notified within the past 7 days
        var cutoff = DateTimeOffset.UtcNow.AddDays(-7);
        var dogsToNotify = diff.NewDogs
            .Where(d =>
            {
                if (firstTimeShelterIds.Contains(d.ShelterId))
                {
                    return false;
                }

                var key = DogDiffEngine.CompositeKey(d);
                return !state.RecentlyNotifiedAids.TryGetValue(key, out var notifiedAt)
                       || notifiedAt < cutoff;
            })
            .ToList();

        if (diff.NewDogs.Count != dogsToNotify.Count)
        {
            logger.LogInformation(
                "Filtered {Count} recently-notified dog(s) from notifications",
                diff.NewDogs.Count - dogsToNotify.Count);
        }

        // Preserve original FirstSeen for returning dogs so the UI "New" badge doesn't reappear
        var stamped = currentDogs
            .Select(d =>
            {
                var key = DogDiffEngine.CompositeKey(d);
                return state.RecentlyNotifiedAids.TryGetValue(key, out var notifiedAt)
                    ? d with { FirstSeen = notifiedAt }
                    : d;
            })
            .ToList();

        var newDogKeys = diff.NewDogs.Select(DogDiffEngine.CompositeKey).ToHashSet();
        var existingDogs = stamped
            .Where(d => !newDogKeys.Contains(DogDiffEngine.CompositeKey(d)))
            .ToList();

        var dogsToStore = await HandleNewDogsAsync(stamped, dogsToNotify, existingDogs, ct);

        if (diff.RemovedAids.Count > 0)
        {
            logger.LogInformation("{Count} dog(s) removed", diff.RemovedAids.Count);
            var adoptedDogs = await dogRepository.GetByKeysAsync(diff.RemovedAids, ct);
            await adoptedDogRepository.SaveAsync(adoptedDogs, DateTimeOffset.UtcNow, ct);
            await dogRepository.RemoveDogsAsync(diff.RemovedAids, ct);
        }

        await dogRepository.UpsertDogsAsync(dogsToStore, ct);
        await SaveCurrentStateAsync(dogsToStore, state.RecentlyNotifiedAids, dogsToNotify, ct);
    }

    private async Task HandleFirstRunAsync(IReadOnlyList<Dog> dogs, CancellationToken ct)
    {
        logger.LogInformation("First run, capturing initial state with {Count} dog(s)", dogs.Count);

        await dogRepository.UpsertDogsAsync(dogs, ct);
        await SaveCurrentStateAsync(dogs, new Dictionary<string, DateTimeOffset>(), [], ct);
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

        var dogsToFetch = newDogs.Concat(backfillDogs)
            .DistinctBy(DogDiffEngine.CompositeKey)
            .ToList();

        var shelterLuvShelterIds = shelterLuvConfigs.Select(s => s.ShelterId)
            .Concat(shelterLuvV3Configs.Select(s => s.ShelterId))
            .ToHashSet();
        var detailTasks = dogsToFetch
            .Select(d => shelterLuvShelterIds.Contains(d.ShelterId)
                ? Task.FromResult<DogDetail?>(null)
                : scrapingEngine.GetDogDetailAsync(d.Aid, d.ShelterId, ct))
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
                IntakeDate = detail.IntakeDate ?? dog.IntakeDate,
                PhotoUrls = detail.PhotoUrls is { Count: > 0 } ? detail.PhotoUrls : dog.PhotoUrls
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
        catch (WebPushException ex) when (ex.StatusCode is HttpStatusCode.BadRequest)
        {
            var body = await ex.HttpResponseMessage.Content.ReadAsStringAsync(ct);
            if (body.Contains("VapidPkHashMismatch"))
            {
                logger.LogWarning("Removing subscription with mismatched VAPID key: {Endpoint}", sub.Endpoint);
                await subscriptionRepository.RemoveByEndpointAsync(sub.Endpoint, ct);
            }
            else
            {
                logger.LogWarning("Transient push failure for {Endpoint}: {Message}", sub.Endpoint, ex.Message);
            }
        }
        catch (WebPushException ex)
        {
            logger.LogWarning("Transient push failure for {Endpoint}: {Message}", sub.Endpoint, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("Network error sending push to {Endpoint}: {Message}", sub.Endpoint, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error sending push to {Endpoint}", sub.Endpoint);
        }
    }

    private async Task SaveCurrentStateAsync(
        IReadOnlyList<Dog> dogs,
        IReadOnlyDictionary<string, DateTimeOffset> previousRecentlyNotified,
        IReadOnlyList<Dog> newlyNotifiedDogs,
        CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.AddDays(-7);
        var now = DateTimeOffset.UtcNow;

        var recentlyNotified = previousRecentlyNotified
            .Where(kv => kv.Value >= cutoff)
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        foreach (var dog in newlyNotifiedDogs)
        {
            recentlyNotified[DogDiffEngine.CompositeKey(dog)] = now;
        }

        var keys = dogs.Select(DogDiffEngine.CompositeKey).ToList();
        var dogMap = dogs.ToDictionary(DogDiffEngine.CompositeKey, d => d.Name ?? "Unknown");
        var state = new SiteState(keys, dogMap, now, recentlyNotified);

        await stateRepository.SaveStateAsync(state, ct);
    }
}
