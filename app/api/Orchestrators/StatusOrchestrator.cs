using Core.DomainModels;
using Core.Repositories;

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

        var allDogs = await dogsTask;
        var state = await stateTask;
        var recentlyAdoptedRaw = await recentlyAdoptedTask;

        var cutoff = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(1));
        var currentKeys = allDogs.Select(d => $"{d.ShelterId}-{d.Aid}").ToHashSet();
        var recentlyAdopted = recentlyAdoptedRaw
            .Where(r => !currentKeys.Contains($"{r.ShelterId}-{r.Aid}") && r.AdoptedAt >= cutoff)
            .ToList();

        var centralNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CentralTimeZone);
        var isActive = centralNow.Hour >= 5 && centralNow.Hour < 20;

        return new StatusResult(allDogs, state?.Updated, isActive, recentlyAdopted);
    }
}
