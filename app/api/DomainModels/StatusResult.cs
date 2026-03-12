namespace Api.DomainModels;

public sealed record StatusResult(
    IReadOnlyList<Dog> Dogs,
    DateTimeOffset? LastChecked,
    bool IsMonitoringActive,
    IReadOnlyList<AdoptedDog> RecentlyAdopted);
