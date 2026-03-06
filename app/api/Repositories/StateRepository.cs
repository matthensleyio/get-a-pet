using System.Text.Json;

using Azure;
using Azure.Data.Tables;

using Api.DomainModels;

namespace Api.Repositories;

public sealed class StateRepository(TableServiceClient tableServiceClient)
{
    private readonly TableClient _tableClient = tableServiceClient.GetTableClient("SiteState");

    private const string PartitionKey = "state";
    private const string RowKey = "latest";

    public async Task<SiteState?> GetStateAsync(CancellationToken ct)
    {
        try
        {
            var response = await _tableClient.GetEntityAsync<TableEntity>(PartitionKey, RowKey, cancellationToken: ct);
            var entity = response.Value;

            var aids = JsonSerializer.Deserialize<List<string>>(entity.GetString("KnownAidsJson") ?? "[]") ?? [];
            var dogs = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.GetString("KnownDogsJson") ?? "{}") ?? [];

            return new SiteState(
                entity.GetInt32("Count") ?? 0,
                aids,
                dogs,
                entity.GetDateTimeOffset("Updated") ?? DateTimeOffset.UtcNow);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task TouchAsync(CancellationToken ct)
    {
        var entity = new TableEntity(PartitionKey, RowKey)
        {
            ["Updated"] = DateTimeOffset.UtcNow
        };
        await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Merge, ct);
    }

    public async Task SaveStateAsync(SiteState state, CancellationToken ct)
    {
        var entity = new TableEntity(PartitionKey, RowKey)
        {
            ["Count"] = state.Count,
            ["KnownAidsJson"] = JsonSerializer.Serialize(state.KnownAids),
            ["KnownDogsJson"] = JsonSerializer.Serialize(state.KnownDogs),
            ["Updated"] = state.Updated
        };

        await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
    }
}
