using Azure;
using Azure.Data.Tables;

using Api.DomainModels;
using Api.Engines;

namespace Api.Repositories;

public sealed class DogRepository(TableServiceClient tableServiceClient)
{
    private readonly TableClient _tableClient = tableServiceClient.GetTableClient("Dogs");

    public async Task<IReadOnlyList<Dog>> GetAllDogsAsync(CancellationToken ct)
    {
        var dogs = new List<Dog>();

        await foreach (var entity in _tableClient.QueryAsync<TableEntity>(cancellationToken: ct))
        {
            dogs.Add(MapToDog(entity));
        }

        return dogs;
    }

    public async Task UpsertDogsAsync(IReadOnlyList<Dog> dogs, CancellationToken ct)
    {
        foreach (var dog in dogs)
        {
            var rowKey = DogDiffEngine.CompositeKey(dog);
            TableEntity? existing = null;

            try
            {
                var response = await _tableClient.GetEntityAsync<TableEntity>("dog", rowKey, cancellationToken: ct);
                existing = response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
            }

            if (existing is not null)
            {
                await _tableClient.DeleteEntityAsync("dog", rowKey, existing.ETag, ct);
            }

            var firstSeen = existing?.GetDateTimeOffset("FirstSeen") ?? DateTimeOffset.UtcNow;
            await _tableClient.AddEntityAsync(BuildEntity(dog, rowKey, firstSeen), ct);
        }
    }

    private static TableEntity BuildEntity(Dog dog, string rowKey, DateTimeOffset firstSeen)
    {
        return new TableEntity("dog", rowKey)
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
            ["FirstSeen"] = firstSeen,
            ["IntakeDate"] = dog.IntakeDate,
            ["ListingDate"] = dog.ListingDate
        };
    }

    public async Task<IReadOnlyList<Dog>> GetByKeysAsync(IReadOnlyList<string> compositeKeys, CancellationToken ct)
    {
        var dogs = new List<Dog>();

        foreach (var key in compositeKeys)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<TableEntity>("dog", key, cancellationToken: ct);
                dogs.Add(MapToDog(response.Value));
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
            }
        }

        return dogs;
    }

    public async Task RemoveDogsAsync(IReadOnlyList<string> compositeKeys, CancellationToken ct)
    {
        foreach (var key in compositeKeys)
        {
            try
            {
                await _tableClient.DeleteEntityAsync("dog", key, cancellationToken: ct);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
            }
        }
    }

    private static Dog MapToDog(TableEntity entity)
    {
        return new Dog(
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
            entity.GetDateTimeOffset("ListingDate"));
    }
}
