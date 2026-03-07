namespace Api.DomainModels;

public sealed record DogDiffResult(
    IReadOnlyList<Dog> NewDogs,
    IReadOnlyList<(string Shelter, string Aid)> RemovedDogs);
