using System.Net.Http.Json;
using System.Text.Json;

using Core.DomainModels;

namespace Core.Engines;

public sealed class ShelterLuvV3ScrapingEngine(
    IHttpClientFactory httpClientFactory,
    IReadOnlyList<ShelterLuvV3Config> shelters)
{
    public async Task<IReadOnlyList<Dog>> GetAllDogsAsync(CancellationToken ct)
    {
        var tasks = shelters
            .Select(shelter => GetDogsForShelterAsync(shelter, ct))
            .ToList();

        var results = await Task.WhenAll(tasks);

        return results.SelectMany(d => d).ToList();
    }

    private async Task<IReadOnlyList<Dog>> GetDogsForShelterAsync(ShelterLuvV3Config shelter, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("ShelterLuv");

        try
        {
            var response = await client.GetFromJsonAsync<ShelterLuvV3Response>(shelter.ApiUrl, ct);

            if (response is null)
            {
                return [];
            }

            return response.Animals
                .Where(animal => String.Equals(animal.Species, "Dog", StringComparison.OrdinalIgnoreCase))
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

    private static Dog MapToDog(ShelterLuvV3Animal animal, ShelterLuvV3Config shelter)
    {
        var breed = animal.SecondaryBreed is { Length: > 0 }
            ? $"{animal.Breed}/{animal.SecondaryBreed}"
            : animal.Breed;

        var color = animal.SecondaryColor is { Length: > 0 }
            ? $"{animal.PrimaryColor}/{animal.SecondaryColor}"
            : animal.PrimaryColor;

        DateTimeOffset? intakeDate = animal.IntakeDate is { } raw && long.TryParse(raw, out var ts)
            ? DateTimeOffset.FromUnixTimeSeconds(ts)
            : null;

        var allPhotos = ExtractAllPhotoUrls(animal.Photos);

        return new Dog(
            animal.UniqueId,
            shelter.ShelterId,
            animal.Name,
            animal.AgeGroup?.Name,
            animal.Sex,
            allPhotos.Count > 0 ? allPhotos[0] : null,
            breed?.Trim(),
            color?.Trim(),
            NormalizeSize(animal.WeightGroup),
            null,
            null,
            animal.Location,
            animal.PublicUrl,
            default,
            intakeDate,
            null,
            allPhotos.Count > 0 ? allPhotos : null);
    }

    private static IReadOnlyList<string> ExtractAllPhotoUrls(JsonElement photos)
    {
        string? coverUrl = null;
        var rest = new List<string>();

        IEnumerable<JsonElement> elements = photos.ValueKind switch
        {
            JsonValueKind.Object => photos.EnumerateObject().Select(p => p.Value),
            JsonValueKind.Array => photos.EnumerateArray(),
            _ => []
        };

        foreach (var el in elements)
        {
            if (!el.TryGetProperty("url", out var urlEl))
            {
                continue;
            }

            var url = urlEl.GetString();
            if (url is null)
            {
                continue;
            }

            if (el.TryGetProperty("isCover", out var isCoverEl) && isCoverEl.GetBoolean())
            {
                coverUrl = url;
            }
            else
            {
                rest.Add(url);
            }
        }

        var result = new List<string>();
        if (coverUrl is not null)
        {
            result.Add(coverUrl);
        }

        result.AddRange(rest);
        return result;
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
