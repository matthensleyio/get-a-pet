using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;

using Api.DomainModels;

namespace Api.Repositories;

public sealed class SubscriptionRepository(DogMonitorDbContext db)
{
    public async Task<IReadOnlyList<PushSubscription>> GetAllAsync(CancellationToken ct)
    {
        var records = await db.PushSubscriptions.AsNoTracking().ToListAsync(ct);

        return records
            .Select(r => new PushSubscription(r.Endpoint, r.P256dh, r.Auth))
            .ToList();
    }

    public async Task AddAsync(PushSubscription sub, CancellationToken ct)
    {
        var hash = ComputeHash(sub.Endpoint);
        var existing = await db.PushSubscriptions.FindAsync([hash], ct);
        if (existing is not null)
        {
            existing.Endpoint = sub.Endpoint;
            existing.P256dh = sub.P256dh;
            existing.Auth = sub.Auth;
        }
        else
        {
            db.PushSubscriptions.Add(new PushSubscriptionRecord
            {
                EndpointHash = hash,
                Endpoint = sub.Endpoint,
                P256dh = sub.P256dh,
                Auth = sub.Auth
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveByEndpointAsync(string endpoint, CancellationToken ct)
    {
        var hash = ComputeHash(endpoint);
        var record = await db.PushSubscriptions.FindAsync([hash], ct);
        if (record is not null)
        {
            db.PushSubscriptions.Remove(record);
            await db.SaveChangesAsync(ct);
        }
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
