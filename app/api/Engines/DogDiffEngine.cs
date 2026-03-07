using Api.DomainModels;

namespace Api.Engines;

public sealed class DogDiffEngine
{
    public DogDiffResult ComputeDiff(IReadOnlyList<Dog> current, SiteState previous)
    {
        // Keys are "{shelter}:{aid}" to avoid cross-shelter ID collisions
        var currentKeys = current.Select(ShelterKey).ToHashSet();
        var previousKeys = previous.KnownAids.ToHashSet();

        var newDogs = current
            .Where(d => !previousKeys.Contains(ShelterKey(d)))
            .ToList();

        var removedDogs = previous.KnownAids
            .Where(key => !currentKeys.Contains(key))
            .Select(key =>
            {
                var colon = key.IndexOf(':');
                return colon > 0
                    ? (key[..colon], key[(colon + 1)..])
                    : ("unknown", key);
            })
            .ToList();

        return new DogDiffResult(newDogs, removedDogs);
    }

    public static string ShelterKey(Dog dog) => $"{dog.Shelter}:{dog.Aid}";
}
