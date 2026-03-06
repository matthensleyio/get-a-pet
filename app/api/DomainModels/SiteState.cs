namespace Api.DomainModels;

public sealed record SiteState(
    int Count,
    IReadOnlyList<string> KnownAids,
    IReadOnlyDictionary<string, string> KnownDogs,
    DateTimeOffset Updated);
