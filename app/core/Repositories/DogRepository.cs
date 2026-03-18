using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Caching.Memory;

using Core.DomainModels;
using Core.Engines;

namespace Core.Repositories;

public sealed class DogRepository(TableServiceClient tableServiceClient, IMemoryCache cache)
{
    private readonly TableClient _tableClient = tableServiceClient.GetTableClient("Dogs");

    private const string DogCacheKey = "all-dogs";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    public async Task<IReadOnlyList<Dog>> GetAllDogsAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(DogCacheKey, out IReadOnlyList<Dog>? cached))
            return cached!;

        var dogs = new List<Dog>();

        await foreach (var entity in _tableClient.QueryAsync<TableEntity>(cancellationToken: ct))
        {
            dogs.Add(MapToDog(entity));
        }

        cache.Set(DogCacheKey, (IReadOnlyList<Dog>)dogs,
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl });

        return dogs;
    }

    public async Task UpsertDogsAsync(IReadOnlyList<Dog> dogs, CancellationToken ct)
    {
        var existingTasks = dogs
            .Select(dog => _tableClient.GetEntityIfExistsAsync<TableEntity>(
                "dog", DogDiffEngine.CompositeKey(dog), cancellationToken: ct))
            .ToList();

        var existingResults = await Task.WhenAll(existingTasks);

        var actions = new List<TableTransactionAction>(dogs.Count);

        for (var i = 0; i < dogs.Count; i++)
        {
            var dog = dogs[i];
            var rowKey = DogDiffEngine.CompositeKey(dog);
            var firstSeen = existingResults[i].HasValue
                ? existingResults[i].Value!.GetDateTimeOffset("FirstSeen") ?? DateTimeOffset.UtcNow
                : dog.FirstSeen != default ? dog.FirstSeen : DateTimeOffset.UtcNow;

            actions.Add(new TableTransactionAction(
                TableTransactionActionType.UpsertReplace, BuildEntity(dog, rowKey, firstSeen)));
        }

        foreach (var batch in actions.Chunk(100))
        {
            await _tableClient.SubmitTransactionAsync(batch, ct);
        }

        cache.Remove(DogCacheKey);
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
        var tasks = compositeKeys
            .Select(key => _tableClient.GetEntityIfExistsAsync<TableEntity>("dog", key, cancellationToken: ct))
            .ToList();

        var results = await Task.WhenAll(tasks);

        return results
            .Where(r => r.HasValue)
            .Select(r => MapToDog(r.Value!))
            .ToList();
    }

    public async Task RemoveDogsAsync(IReadOnlyList<string> compositeKeys, CancellationToken ct)
    {
        var deleteTasks = compositeKeys.Select(async key =>
        {
            try
            {
                await _tableClient.DeleteEntityAsync("dog", key, ETag.All, ct);
            }
            catch (RequestFailedException ex) when (ex.Status == 404)
            {
            }
        });

        await Task.WhenAll(deleteTasks);
        cache.Remove(DogCacheKey);
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
