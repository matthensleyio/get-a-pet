using System.Net;

using Microsoft.Extensions.Logging;

using Api.DomainModels;
using Api.Engines;
using Api.Repositories;
using WebPush;

namespace Api.Orchestrators;

public sealed class MonitorOrchestrator(
    ScrapingEngine scrapingEngine,
    PetfinderEngine petfinderEngine,
    DogDiffEngine dogDiffEngine,
    NotificationEngine notificationEngine,
    StateRepository stateRepository,
    DogRepository dogRepository,
    SubscriptionRepository subscriptionRepository,
    ILogger<MonitorOrchestrator> logger)
{
    // Only re-fetch Petfinder orgs at most this often to stay within API rate limits
    private static readonly TimeSpan PetfinderCooldown = TimeSpan.FromMinutes(15);

    private static readonly (string OrgId, string ShelterName)[] PetfinderOrgs =
    [
        ("MO179", "Humane Society of Missouri"),
        ("MO208", "Humane Society of Missouri"),
        ("MO579", "KC Pet Project"),
        ("KS07",  "Great Plains SPCA"),
    ];

    public async Task CheckAsync(CancellationToken ct)
    {
        var state = await stateRepository.GetStateAsync(ct);

        var khsDogsTask = scrapingEngine.GetAllDogsAsync(ct);
        var petfinderDogsTask = GetPetfinderDogsAsync(state, ct);

        await Task.WhenAll(khsDogsTask, petfinderDogsTask);

        var (petfinderDogs, petfinderFetchedAt) = await petfinderDogsTask;
        var currentDogs = (await khsDogsTask).Concat(petfinderDogs).ToList();

        if (state is null)
        {
            await HandleFirstRunAsync(currentDogs, petfinderFetchedAt, ct);
            return;
        }

        var diff = dogDiffEngine.ComputeDiff(currentDogs, state);

        var aidsNeedingDetails = await dogRepository.GetAidsNeedingDetailsAsync(ct);
        var aidsNeedingDetailsSet = aidsNeedingDetails
            .Where(x => x.Shelter == "Kansas Humane Society")
            .Select(x => x.Aid)
            .ToHashSet();

        var newDogAids = diff.NewDogs.Select(d => d.Aid).ToHashSet();
        var backfillDogs = currentDogs
            .Where(d => d.Shelter == "Kansas Humane Society"
                     && !newDogAids.Contains(d.Aid)
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
        await SaveCurrentStateAsync(dogsToStore, petfinderFetchedAt ?? state.LastPetfinderFetch, ct);
    }

    private async Task<(IReadOnlyList<Dog> Dogs, DateTimeOffset? FetchedAt)> GetPetfinderDogsAsync(
        SiteState? state,
        CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        if (state?.LastPetfinderFetch is { } last && (now - last) < PetfinderCooldown)
        {
            logger.LogInformation("Petfinder cooldown active; reusing cached data");
            // Return dogs from DB that came from Petfinder shelters
            var allStored = await dogRepository.GetAllDogsAsync(ct);
            var petfinderShelters = PetfinderOrgs.Select(o => o.ShelterName).ToHashSet();
            return (allStored.Where(d => petfinderShelters.Contains(d.Shelter)).ToList(), null);
        }

        logger.LogInformation("Fetching from {Count} Petfinder org(s)", PetfinderOrgs.Length);

        // Deduplicate by shelter name — MO179 and MO208 both map to "Humane Society of Missouri",
        // so we query each org but merge by (ShelterName, Aid)
        var tasks = PetfinderOrgs
            .Select(o => petfinderEngine.GetAllDogsAsync(o.OrgId, o.ShelterName, ct))
            .ToList();

        var results = await Task.WhenAll(tasks);

        var seen = new HashSet<string>();
        var merged = new List<Dog>();

        foreach (var dog in results.SelectMany(r => r))
        {
            var key = $"{dog.Shelter}:{dog.Aid}";
            if (seen.Add(key))
            {
                merged.Add(dog);
            }
        }

        return (merged, now);
    }

    private async Task HandleFirstRunAsync(
        IReadOnlyList<Dog> dogs,
        DateTimeOffset? petfinderFetchedAt,
        CancellationToken ct)
    {
        logger.LogInformation("First run, capturing initial state with {Count} dog(s)", dogs.Count);

        await dogRepository.UpsertDogsAsync(dogs, ct);
        await SaveCurrentStateAsync(dogs, petfinderFetchedAt, ct);
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

        // Only KHS dogs need a separate detail fetch; Petfinder dogs have full data already
        var khsDogsToFetch = newDogs
            .Where(d => d.Shelter == "Kansas Humane Society")
            .Concat(backfillDogs)
            .ToList();

        Dictionary<string, Dog> enrichedDogs = [];

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

    private async Task SaveCurrentStateAsync(
        IReadOnlyList<Dog> dogs,
        DateTimeOffset? lastPetfinderFetch,
        CancellationToken ct)
    {
        var aids    = dogs.Select(DogDiffEngine.ShelterKey).ToList();
        var dogMap  = dogs.ToDictionary(DogDiffEngine.ShelterKey, d => d.Name ?? "Unknown");
        var state   = new SiteState(aids, dogMap, DateTimeOffset.UtcNow, lastPetfinderFetch);

        await stateRepository.SaveStateAsync(state, ct);
    }
}
