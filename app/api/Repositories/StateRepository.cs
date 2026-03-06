using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Api.DomainModels;

namespace Api.Repositories;

public sealed class StateRepository(DogMonitorDbContext db)
{
    public async Task<SiteState?> GetStateAsync(CancellationToken ct)
    {
        var record = await db.SiteStates.FirstOrDefaultAsync(ct);
        if (record is null)
        {
            return null;
        }

        var aids = JsonSerializer.Deserialize<List<string>>(record.KnownAidsJson) ?? [];
        var dogs = JsonSerializer.Deserialize<Dictionary<string, string>>(record.KnownDogsJson) ?? [];

        return new SiteState(record.Count, aids, dogs, record.Updated);
    }

    public async Task SaveStateAsync(SiteState state, CancellationToken ct)
    {
        var record = await db.SiteStates.FirstOrDefaultAsync(ct);
        if (record is null)
        {
            record = new SiteStateRecord();
            db.SiteStates.Add(record);
        }

        record.Count = state.Count;
        record.KnownAidsJson = JsonSerializer.Serialize(state.KnownAids);
        record.KnownDogsJson = JsonSerializer.Serialize(state.KnownDogs);
        record.Updated = state.Updated;

        await db.SaveChangesAsync(ct);
    }
}
