namespace Core.DomainModels;

public sealed record DogDiffResult(
    IReadOnlyList<Dog> NewDogs,
    IReadOnlyList<string> RemovedAids);
