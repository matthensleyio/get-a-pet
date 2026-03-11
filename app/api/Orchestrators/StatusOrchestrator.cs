using System.Text.RegularExpressions;

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

    public async Task<StatusResult> GetStatusAsync(
        string sort,
        int page,
        int pageSize,
        IReadOnlyList<string> shelterIds,
        IReadOnlyList<string> favoriteKeys,
        CancellationToken ct)
    {
        var dogsTask = dogRepository.GetAllDogsAsync(ct);
        var stateTask = stateRepository.GetStateAsync(ct);
        var recentlyAdoptedTask = adoptedDogRepository.GetRecentAsync(ct);

        await Task.WhenAll(dogsTask, stateTask, recentlyAdoptedTask);

        var allDogs = await dogsTask;
        var state = await stateTask;
        var recentlyAdoptedRaw = await recentlyAdoptedTask;

        var currentKeys = allDogs.Select(d => $"{d.ShelterId}-{d.Aid}").ToHashSet();
        var recentlyAdopted = recentlyAdoptedRaw
            .Where(r => !currentKeys.Contains($"{r.ShelterId}-{r.Aid}"))
            .ToList();

        var centralNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CentralTimeZone);
        var isActive = centralNow.Hour >= 5 && centralNow.Hour < 20;

        var filtered = shelterIds.Count > 0
            ? allDogs.Where(d => shelterIds.Contains(d.ShelterId)).ToList()
            : allDogs.ToList();

        var sorted = SortDogs(filtered, sort);
        var totalCount = sorted.Count;
        var pageDogs = sorted.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        IReadOnlyList<Dog> favDogs = [];
        IReadOnlyList<AdoptedDog> favAdopted = [];

        if (favoriteKeys.Count > 0)
        {
            favDogs = allDogs.Where(d => favoriteKeys.Contains($"{d.ShelterId}-{d.Aid}")).ToList();
            favAdopted = recentlyAdopted.Where(d => favoriteKeys.Contains($"{d.ShelterId}-{d.Aid}")).ToList();
        }

        return new StatusResult(pageDogs, totalCount, page, pageSize, state?.Updated, isActive, recentlyAdopted, favDogs, favAdopted);
    }

    private static IReadOnlyList<Dog> SortDogs(IReadOnlyList<Dog> dogs, string sort)
    {
        return sort switch
        {
            "age" => dogs.OrderBy(d => ParseAgeMonths(d.Age)).ThenBy(d => d.Name ?? "").ToList(),
            "name" => dogs.OrderBy(d => d.Name ?? "").ToList(),
            _ => dogs.OrderByDescending(EffectiveSortDate).ThenBy(d => d.Name ?? "").ToList()
        };
    }

    private static DateTimeOffset EffectiveSortDate(Dog dog)
    {
        if (dog.ListingDate.HasValue && DateTimeOffset.UtcNow - dog.ListingDate.Value < TimeSpan.FromDays(1))
            return dog.ListingDate.Value;

        return dog.IntakeDate ?? dog.ListingDate ?? dog.FirstSeen;
    }

    private static int ParseAgeMonths(string? age)
    {
        if (age is null) return int.MaxValue;

        var s = age.ToLowerInvariant();
        var yearMatch = Regex.Match(s, @"(\d+)\s*year");
        var monthMatch = Regex.Match(s, @"(\d+)\s*month");
        var years = yearMatch.Success ? int.Parse(yearMatch.Groups[1].Value) : 0;
        var months = monthMatch.Success ? int.Parse(monthMatch.Groups[1].Value) : 0;

        return years == 0 && months == 0 ? int.MaxValue : years * 12 + months;
    }
}
