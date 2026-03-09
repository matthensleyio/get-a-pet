using Azure;
using Azure.Data.Tables;

using Api.DomainModels;

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
            TableEntity? existing = null;

            try
            {
                var response = await _tableClient.GetEntityAsync<TableEntity>("dog", dog.Aid, cancellationToken: ct);
                existing = response.Value;
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
            }

            if (existing is not null)
            {
                await _tableClient.DeleteEntityAsync("dog", dog.Aid, existing.ETag, ct);
            }

            var firstSeen = existing?.GetDateTimeOffset("FirstSeen") ?? DateTimeOffset.UtcNow;
            await _tableClient.AddEntityAsync(BuildEntity(dog, firstSeen), ct);
        }
    }

    private static TableEntity BuildEntity(Dog dog, DateTimeOffset firstSeen)
    {
        return new TableEntity("dog", dog.Aid)
        {
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

    public async Task<IReadOnlyList<Dog>> GetByAidsAsync(IReadOnlyList<string> aids, CancellationToken ct)
    {
        var dogs = new List<Dog>();

        foreach (var aid in aids)
        {
            try
            {
                var response = await _tableClient.GetEntityAsync<TableEntity>("dog", aid, cancellationToken: ct);
                dogs.Add(MapToDog(response.Value));
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
            }
        }

        return dogs;
    }

    public async Task RemoveDogsAsync(IReadOnlyList<string> aids, CancellationToken ct)
    {
        foreach (var aid in aids)
        {
            try
            {
                await _tableClient.DeleteEntityAsync("dog", aid, cancellationToken: ct);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
            }
        }
    }

    private static Dog MapToDog(TableEntity entity)
    {
        return new Dog(
            entity.RowKey,
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
