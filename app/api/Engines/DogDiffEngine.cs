using Api.DomainModels;

namespace Api.Engines;

public sealed class DogDiffEngine
{
    public DogDiffResult ComputeDiff(IReadOnlyList<Dog> current, SiteState previous)
    {
        var currentKeys = current.Select(d => CompositeKey(d)).ToHashSet();
        var previousKeys = previous.KnownAids.ToHashSet();

        var newDogs = current
            .Where(d => !previousKeys.Contains(CompositeKey(d)))
            .ToList();

        var removedAids = previous.KnownAids
            .Where(key => !currentKeys.Contains(key))
            .ToList();

        return new DogDiffResult(newDogs, removedAids);
    }

    public static string CompositeKey(Dog dog) => $"{dog.ShelterId}-{dog.Aid}";
}
