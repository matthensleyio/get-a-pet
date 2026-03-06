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

            if (existing is null)
            {
                var entity = new TableEntity("dog", dog.Aid)
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
                    ["FirstSeen"] = DateTimeOffset.UtcNow,
                    ["IntakeDate"] = dog.IntakeDate
                };

                await _tableClient.AddEntityAsync(entity, ct);
            }
            else
            {
                existing["Name"] = dog.Name;
                existing["Age"] = dog.Age;
                existing["Gender"] = dog.Gender;
                existing["PhotoUrl"] = dog.PhotoUrl;
                existing["ProfileUrl"] = dog.ProfileUrl;
                existing["IntakeDate"] = dog.IntakeDate;

                if (dog.Breed is not null) existing["Breed"] = dog.Breed;
                if (dog.Color is not null) existing["Color"] = dog.Color;
                if (dog.Size is not null) existing["Size"] = dog.Size;
                if (dog.Weight is not null) existing["Weight"] = dog.Weight;
                if (dog.AdoptionFee is not null) existing["AdoptionFee"] = dog.AdoptionFee;
                if (dog.CurrentLocation is not null) existing["CurrentLocation"] = dog.CurrentLocation;

                await _tableClient.UpdateEntityAsync(existing, existing.ETag, TableUpdateMode.Merge, ct);
            }
        }
    }

    public async Task<IReadOnlyList<string>> GetAidsNeedingDetailsAsync(CancellationToken ct)
    {
        var aids = new List<string>();

        await foreach (var entity in _tableClient.QueryAsync<TableEntity>(
            select: ["RowKey", "Breed"],
            cancellationToken: ct))
        {
            if (entity.GetString("Breed") is null)
            {
                aids.Add(entity.RowKey);
            }
        }

        return aids;
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
            entity.GetDateTimeOffset("IntakeDate"));
    }
}
