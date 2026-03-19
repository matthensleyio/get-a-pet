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

        return new Dog(
            animal.UniqueId,
            shelter.ShelterId,
            animal.Name,
            animal.AgeGroup?.Name,
            animal.Sex,
            ExtractCoverPhoto(animal.Photos),
            breed?.Trim(),
            color?.Trim(),
            NormalizeSize(animal.WeightGroup),
            null,
            null,
            animal.Location,
            animal.PublicUrl,
            default,
            intakeDate,
            null);
    }

    private static string? ExtractCoverPhoto(JsonElement photos)
    {
        if (photos.ValueKind == JsonValueKind.Object)
        {
            string? firstUrl = null;
            foreach (var prop in photos.EnumerateObject())
            {
                if (!prop.Value.TryGetProperty("url", out var urlEl))
                {
                    continue;
                }

                var url = urlEl.GetString();
                firstUrl ??= url;

                if (prop.Value.TryGetProperty("isCover", out var isCoverEl) && isCoverEl.GetBoolean())
                {
                    return url;
                }
            }
            return firstUrl;
        }

        if (photos.ValueKind == JsonValueKind.Array)
        {
            string? firstUrl = null;
            foreach (var photo in photos.EnumerateArray())
            {
                if (!photo.TryGetProperty("url", out var urlEl))
                {
                    continue;
                }

                var url = urlEl.GetString();
                firstUrl ??= url;

                if (photo.TryGetProperty("isCover", out var isCoverEl) && isCoverEl.GetBoolean())
                {
                    return url;
                }
            }
            return firstUrl;
        }

        return null;
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
