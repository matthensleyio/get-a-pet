namespace Api.DomainModels;

public sealed record StatusResult(
    IReadOnlyList<Dog> Dogs,
    int Count,
    DateTimeOffset? LastChecked,
    bool IsMonitoringActive,
    IReadOnlyList<AdoptedDog> RecentlyAdopted);
