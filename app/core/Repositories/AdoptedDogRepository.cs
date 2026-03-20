using System.Text.Json;

using Azure;
using Azure.Data.Tables;

using Core.DomainModels;
using Core.Engines;

namespace Core.Repositories;

public sealed class AdoptedDogRepository(TableServiceClient tableServiceClient)
{
    private readonly TableClient _tableClient = tableServiceClient.GetTableClient("AdoptedDogs");

    private const string PartitionKey = "adopted";
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(7);

    public async Task<IReadOnlyList<AdoptedDog>> GetRecentAsync(CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(MaxAge);
        var dogs = new List<AdoptedDog>();

        await foreach (var entity in _tableClient.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{PartitionKey}'",
            cancellationToken: ct))
        {
            var adoptedAt = entity.GetDateTimeOffset("AdoptedAt") ?? DateTimeOffset.MinValue;
            if (adoptedAt >= cutoff)
            {
                dogs.Add(MapToAdoptedDog(entity));
            }
        }

        return dogs;
    }

    public async Task SaveAsync(IReadOnlyList<Dog> dogs, DateTimeOffset adoptedAt, CancellationToken ct)
    {
        foreach (var dog in dogs)
        {
            var rowKey = DogDiffEngine.CompositeKey(dog);
            var entity = new TableEntity(PartitionKey, rowKey)
            {
                ["Aid"] = dog.Aid,
                ["ShelterId"] = dog.ShelterId,
                ["Name"] = dog.Name,
                ["Age"] = dog.Age,
                ["Gender"] = dog.Gender,
                ["PhotoUrl"] = dog.PhotoUrl,
                ["Breed"] = dog.Breed,
                ["Color"] = dog.Color,
                ["Size"] = dog.Size,
                ["Weight"] = dog.Weight,
                ["AdoptionFee"] = dog.AdoptionFee,
                ["CurrentLocation"] = dog.CurrentLocation,
                ["ProfileUrl"] = dog.ProfileUrl,
                ["FirstSeen"] = dog.FirstSeen,
                ["IntakeDate"] = dog.IntakeDate,
                ["AdoptedAt"] = adoptedAt,
                ["PhotoUrls"] = dog.PhotoUrls is { Count: > 0 } ? JsonSerializer.Serialize(dog.PhotoUrls) : null
            };

            await _tableClient.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);
        }
    }

    public async Task PruneOldAsync(CancellationToken ct)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(MaxAge);
        var toDelete = new List<(string PartitionKey, string RowKey)>();

        await foreach (var entity in _tableClient.QueryAsync<TableEntity>(
            filter: $"PartitionKey eq '{PartitionKey}'",
            select: ["PartitionKey", "RowKey", "AdoptedAt"],
            cancellationToken: ct))
        {
            var adoptedAt = entity.GetDateTimeOffset("AdoptedAt") ?? DateTimeOffset.MinValue;
            if (adoptedAt < cutoff)
            {
                toDelete.Add((entity.PartitionKey, entity.RowKey));
            }
        }

        foreach (var (pk, rk) in toDelete)
        {
            try
            {
                await _tableClient.DeleteEntityAsync(pk, rk, cancellationToken: ct);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
            }
        }
    }

    private static AdoptedDog MapToAdoptedDog(TableEntity entity)
    {
        var photoUrlsJson = entity.GetString("PhotoUrls");
        var photoUrls = photoUrlsJson is not null
            ? JsonSerializer.Deserialize<List<string>>(photoUrlsJson)
            : null;

        return new AdoptedDog(
            entity.GetString("Aid") ?? entity.RowKey,
            entity.GetString("ShelterId") ?? "khs",
            entity.GetString("Name"),
            entity.GetString("Age"),
            entity.GetString("Gender"),
            entity.GetString("PhotoUrl"),
            entity.GetString("Breed"),
            entity.GetString("Color"),
            entity.GetString("Size"),
            entity.GetString("Weight"),
            entity.GetString("AdoptionFee"),
            entity.GetString("CurrentLocation"),
            entity.GetString("ProfileUrl"),
            entity.GetDateTimeOffset("FirstSeen") ?? DateTimeOffset.UtcNow,
            entity.GetDateTimeOffset("IntakeDate"),
            entity.GetDateTimeOffset("AdoptedAt") ?? DateTimeOffset.UtcNow,
            photoUrls is { Count: > 0 } ? photoUrls : null);
    }
}
