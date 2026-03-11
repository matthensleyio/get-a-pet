namespace Api.DomainModels;

public sealed record StatusResult(
    IReadOnlyList<Dog> Dogs,
    int TotalCount,
    int Page,
    int PageSize,
    DateTimeOffset? LastChecked,
    bool IsMonitoringActive,
    IReadOnlyList<AdoptedDog> RecentlyAdopted,
    IReadOnlyList<Dog> FavoritedDogs,
    IReadOnlyList<AdoptedDog> FavoritedAdoptedDogs);
