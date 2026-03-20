using System.Security.Cryptography;
using System.Text;

using Azure;
using Azure.Data.Tables;

using Core.DomainModels;

namespace Core.Repositories;

public sealed class FavoritesRepository(TableServiceClient tableServiceClient)
{
    private readonly TableClient _tableClient = tableServiceClient.GetTableClient("Favorites");

    public async Task AddAsync(string endpoint, string aid, string shelterId, CancellationToken ct)
    {
        var hash = ComputeHash(endpoint);
        var rowKey = $"{shelterId}-{aid}";
        var entity = new TableEntity(hash, rowKey)
        {
            ["Aid"] = aid,
            ["ShelterId"] = shelterId
        };
        await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
    }

    public async Task RemoveAsync(string endpoint, string aid, string shelterId, CancellationToken ct)
    {
        var hash = ComputeHash(endpoint);
        var rowKey = $"{shelterId}-{aid}";
        try
        {
            await _tableClient.DeleteEntityAsync(hash, rowKey, cancellationToken: ct);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
        }
    }

    public async Task<IReadOnlyList<Favorite>> GetByEndpointAsync(string endpoint, CancellationToken ct)
    {
        var hash = ComputeHash(endpoint);
        var favorites = new List<Favorite>();

        await foreach (var entity in _tableClient.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{hash}'",
            cancellationToken: ct))
        {
            favorites.Add(new Favorite(
                hash,
                entity.GetString("Aid") ?? string.Empty,
                entity.GetString("ShelterId") ?? string.Empty));
        }

        return favorites;
    }

    public async Task<IReadOnlyList<string>> GetSubscriptionHashesByDogKeyAsync(string dogKey, CancellationToken ct)
    {
        var hashes = new List<string>();

        await foreach (var entity in _tableClient.QueryAsync<TableEntity>(
            filter: $"RowKey eq '{dogKey}'",
            select: ["PartitionKey"],
            cancellationToken: ct))
        {
            hashes.Add(entity.PartitionKey);
        }

        return hashes;
    }

    public async Task RemoveByEndpointAsync(string endpoint, CancellationToken ct)
    {
        var hash = ComputeHash(endpoint);
        var toDelete = new List<string>();

        await foreach (var entity in _tableClient.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{hash}'",
            select: ["PartitionKey", "RowKey"],
            cancellationToken: ct))
        {
            toDelete.Add(entity.RowKey);
        }

        foreach (var rowKey in toDelete)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(hash, rowKey, cancellationToken: ct);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
            }
        }
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
