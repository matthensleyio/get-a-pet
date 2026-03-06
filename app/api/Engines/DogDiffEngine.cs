using Api.DomainModels;

namespace Api.Engines;

public sealed class DogDiffEngine
{
    public DogDiffResult ComputeDiff(IReadOnlyList<Dog> current, SiteState previous)
    {
        var currentAids = current.Select(d => d.Aid).ToHashSet();
        var previousAids = previous.KnownAids.ToHashSet();

        var newDogs = current
            .Where(d => !previousAids.Contains(d.Aid))
            .ToList();

        var removedAids = previous.KnownAids
            .Where(aid => !currentAids.Contains(aid))
            .ToList();

        return new DogDiffResult(newDogs, removedAids);
    }
}
