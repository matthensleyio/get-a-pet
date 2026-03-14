namespace Core.DomainModels;

public sealed record SiteState(
    IReadOnlyList<string> KnownAids,
    IReadOnlyDictionary<string, string> KnownDogs,
    DateTimeOffset Updated);
