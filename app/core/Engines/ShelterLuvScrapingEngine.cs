using System.Net.Http.Json;
using System.Text.Json;

using Core.DomainModels;

namespace Core.Engines;

public sealed class ShelterLuvScrapingEngine(
    IHttpClientFactory httpClientFactory,
    IReadOnlyList<ShelterLuvConfig> shelters)
{
    public async Task<IReadOnlyList<Dog>> GetAllDogsAsync(CancellationToken ct)
    {
        var tasks = shelters
            .Select(shelter => GetDogsForShelterAsync(shelter, ct))
            .ToList();

        var results = await Task.WhenAll(tasks);

        return results.SelectMany(d => d).ToList();
    }

    private async Task<IReadOnlyList<Dog>> GetDogsForShelterAsync(ShelterLuvConfig shelter, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("ShelterLuv");

        try
        {
            var animals = await client.GetFromJsonAsync<IReadOnlyList<ShelterLuvAnimal>>(shelter.ApiUrl, ct);

            if (animals is null)
            {
                return [];
            }

            return animals
                .Select(animal => MapToDog(animal, shelter))
                .ToList();
        }
        catch (HttpRequestException)
        {
            return [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static Dog MapToDog(ShelterLuvAnimal animal, ShelterLuvConfig shelter)
    {
        var breed = animal.SecondaryBreed is { Length: > 0 }
            ? $"{animal.PrimaryBreed}/{animal.SecondaryBreed}"
            : animal.PrimaryBreed;

        var color = animal.SecondaryColor is { Length: > 0 }
            ? $"{animal.PrimaryColor}/{animal.SecondaryColor}"
            : animal.PrimaryColor;

        var adoptionFee = animal.Price.ValueKind switch
        {
            JsonValueKind.Number => $"${animal.Price.GetInt32()}",
            JsonValueKind.String => animal.Price.GetString(),
            _ => null
        };

        DateTimeOffset? intakeDate = animal.IntakeDate > 0
            ? DateTimeOffset.FromUnixTimeSeconds(animal.IntakeDate)
            : null;

        var profileUrl = String.Format(shelter.ProfileUrlTemplate, animal.Id);

        return new Dog(
            animal.Id,
            shelter.ShelterId,
            animal.Name,
            animal.Age,
            animal.Gender,
            animal.Cover,
            breed?.Trim(),
            color?.Trim(),
            NormalizeSize(animal.Size),
            null,
            adoptionFee,
            null,
            profileUrl,
            default,
            intakeDate,
            null);
    }

    private static string? NormalizeSize(string? size)
    {
        if (size is null)
        {
            return null;
        }

        var parenIdx = size.IndexOf('(');
        return parenIdx > 0 ? size[..parenIdx].Trim() : size.Trim();
    }
}
