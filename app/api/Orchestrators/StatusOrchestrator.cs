using Api.DomainModels;
using Api.Repositories;

namespace Api.Orchestrators;

public sealed class StatusOrchestrator(
    DogRepository dogRepository,
    StateRepository stateRepository,
    AdoptedDogRepository adoptedDogRepository)
{
    private static readonly TimeZoneInfo CentralTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");

    public async Task<StatusResult> GetStatusAsync(CancellationToken ct)
    {
        var dogsTask = dogRepository.GetAllDogsAsync(ct);
        var stateTask = stateRepository.GetStateAsync(ct);
        var recentlyAdoptedTask = adoptedDogRepository.GetRecentAsync(ct);

        await Task.WhenAll(dogsTask, stateTask, recentlyAdoptedTask);

        var dogs = await dogsTask;
        var state = await stateTask;
        var recentlyAdoptedRaw = await recentlyAdoptedTask;
        var currentAids = dogs.Select(d => d.Aid).ToHashSet();
        var recentlyAdopted = recentlyAdoptedRaw.Where(r => !currentAids.Contains(r.Aid)).ToList();
        var centralNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CentralTimeZone);
        var isActive = centralNow.Hour >= 5 && centralNow.Hour < 20;

        return new StatusResult(dogs, dogs.Count, state?.Updated, isActive, recentlyAdopted);
    }
}
