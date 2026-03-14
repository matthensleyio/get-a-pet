using System.Security.Cryptography;
using System.Text;

using Azure;
using Azure.Data.Tables;

using Core.DomainModels;

namespace Core.Repositories;

public sealed class SubscriptionRepository(TableServiceClient tableServiceClient)
{
    private readonly TableClient _tableClient = tableServiceClient.GetTableClient("PushSubscriptions");

    private const string PartitionKey = "sub";

    public async Task<IReadOnlyList<PushSubscription>> GetAllAsync(CancellationToken ct)
    {
        var subscriptions = new List<PushSubscription>();

        await foreach (var entity in _tableClient.QueryAsync<TableEntity>(cancellationToken: ct))
        {
            var shelterIdsRaw = entity.GetString("ShelterIds") ?? string.Empty;
            var shelterIds = shelterIdsRaw.Length > 0
                ? shelterIdsRaw.Split(',').Where(s => s.Length > 0).ToList()
                : (IReadOnlyList<string>)[];
            subscriptions.Add(new PushSubscription(
                entity.GetString("Endpoint") ?? string.Empty,
                entity.GetString("P256dh") ?? string.Empty,
                entity.GetString("Auth") ?? string.Empty,
                shelterIds));
        }

        return subscriptions;
    }

    public async Task AddAsync(PushSubscription sub, CancellationToken ct)
    {
        var hash = ComputeHash(sub.Endpoint);

        var entity = new TableEntity(PartitionKey, hash)
        {
            ["Endpoint"] = sub.Endpoint,
            ["P256dh"] = sub.P256dh,
            ["Auth"] = sub.Auth,
            ["ShelterIds"] = string.Join(',', sub.ShelterIds)
        };

        await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
    }

    public async Task RemoveByEndpointAsync(string endpoint, CancellationToken ct)
    {
        var hash = ComputeHash(endpoint);

        try
        {
            await _tableClient.DeleteEntityAsync(PartitionKey, hash, cancellationToken: ct);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
        }
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
