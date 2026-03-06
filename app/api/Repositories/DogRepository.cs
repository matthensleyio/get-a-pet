using Microsoft.EntityFrameworkCore;

using Api.DomainModels;

namespace Api.Repositories;

public sealed class DogRepository(DogMonitorDbContext db)
{
    public async Task<IReadOnlyList<Dog>> GetAllDogsAsync(CancellationToken ct)
    {
        var records = await db.Dogs.AsNoTracking().ToListAsync(ct);

        return records
            .Select(r => new Dog(r.Aid, r.Name, r.Age, r.Gender, r.PhotoUrl, r.Breed, r.ProfileUrl))
            .ToList();
    }

    public async Task UpsertDogsAsync(IReadOnlyList<Dog> dogs, CancellationToken ct)
    {
        foreach (var dog in dogs)
        {
            var existing = await db.Dogs.FindAsync([dog.Aid], ct);
            if (existing is null)
            {
                db.Dogs.Add(new DogRecord
                {
                    Aid = dog.Aid,
                    Name = dog.Name,
                    Age = dog.Age,
                    Gender = dog.Gender,
                    PhotoUrl = dog.PhotoUrl,
                    Breed = dog.Breed,
                    ProfileUrl = dog.ProfileUrl,
                    FirstSeen = DateTimeOffset.UtcNow
                });
            }
            else
            {
                existing.Name = dog.Name;
                existing.Age = dog.Age;
                existing.Gender = dog.Gender;
                existing.PhotoUrl = dog.PhotoUrl;
                existing.Breed = dog.Breed;
                existing.ProfileUrl = dog.ProfileUrl;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveDogsAsync(IReadOnlyList<string> aids, CancellationToken ct)
    {
        var records = await db.Dogs
            .Where(r => aids.Contains(r.Aid))
            .ToListAsync(ct);

        db.Dogs.RemoveRange(records);
        await db.SaveChangesAsync(ct);
    }
}
