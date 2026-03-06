using Microsoft.Extensions.Logging;

using Api.DomainModels;
using Api.Engines;
using Api.Repositories;

namespace Api.Orchestrators;

public sealed class MonitorOrchestrator(
    ScrapingEngine scrapingEngine,
    DogDiffEngine dogDiffEngine,
    NotificationEngine notificationEngine,
    StateRepository stateRepository,
    DogRepository dogRepository,
    SubscriptionRepository subscriptionRepository,
    ILogger<MonitorOrchestrator> logger)
{
    public async Task CheckAsync(CancellationToken ct)
    {
        var currentCount = await scrapingEngine.GetDogCountAsync(ct);
        var state = await stateRepository.GetStateAsync(ct);

        if (state is null)
        {
            await HandleFirstRunAsync(currentCount, ct);
            return;
        }

        if (currentCount == state.Count)
        {
            await stateRepository.TouchAsync(ct);
            return;
        }

        logger.LogInformation("Count changed from {Old} to {New}, checking for changes", state.Count, currentCount);

        var currentDogs = await scrapingEngine.GetAllDogsAsync(ct);
        var diff = dogDiffEngine.ComputeDiff(currentDogs, state);

        if (diff.NewDogs.Count > 0)
        {
            await HandleNewDogsAsync(diff.NewDogs, ct);
        }

        if (diff.RemovedAids.Count > 0)
        {
            logger.LogInformation("{Count} dog(s) removed", diff.RemovedAids.Count);
            await dogRepository.RemoveDogsAsync(diff.RemovedAids, ct);
        }

        await dogRepository.UpsertDogsAsync(currentDogs, ct);
        await SaveCurrentStateAsync(currentCount, currentDogs, ct);
    }

    private async Task HandleFirstRunAsync(int count, CancellationToken ct)
    {
        logger.LogInformation("First run, capturing initial state with count {Count}", count);

        var dogs = await scrapingEngine.GetAllDogsAsync(ct);
        await dogRepository.UpsertDogsAsync(dogs, ct);
        await SaveCurrentStateAsync(count, dogs, ct);
    }

    private async Task HandleNewDogsAsync(IReadOnlyList<Dog> newDogs, CancellationToken ct)
    {
        logger.LogInformation("Found {Count} new dog(s)", newDogs.Count);

        var breedTasks = newDogs
            .Select(d => scrapingEngine.GetDogBreedAsync(d.Aid, ct))
            .ToList();

        var breeds = await Task.WhenAll(breedTasks);

        var dogsWithBreeds = newDogs
            .Select((dog, i) => dog with { Breed = breeds[i] })
            .ToList();

        var subscriptions = await subscriptionRepository.GetAllAsync(ct);

        foreach (var dog in dogsWithBreeds)
        {
            var payload = notificationEngine.BuildPayload(dog);
            var sendTasks = subscriptions
                .Select(sub => SendAndCleanupAsync(payload, sub, ct))
                .ToList();

            await Task.WhenAll(sendTasks);
        }
    }

    private async Task SendAndCleanupAsync(
        NotificationPayload payload,
        DomainModels.PushSubscription sub,
        CancellationToken ct)
    {
        var success = await notificationEngine.SendAsync(payload, sub, ct);
        if (!success)
        {
            logger.LogWarning("Removing dead subscription: {Endpoint}", sub.Endpoint);
            await subscriptionRepository.RemoveByEndpointAsync(sub.Endpoint, ct);
        }
    }

    private async Task SaveCurrentStateAsync(
        int count,
        IReadOnlyList<Dog> dogs,
        CancellationToken ct)
    {
        var aids = dogs.Select(d => d.Aid).ToList();
        var dogMap = dogs.ToDictionary(d => d.Aid, d => d.Name ?? "Unknown");
        var state = new SiteState(count, aids, dogMap, DateTimeOffset.UtcNow);

        await stateRepository.SaveStateAsync(state, ct);
    }
}
