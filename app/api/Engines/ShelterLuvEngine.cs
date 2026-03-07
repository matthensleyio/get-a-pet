using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Api.DomainModels;

namespace Api.Engines;

public sealed class ShelterLuvEngine(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ShelterLuvEngine> logger)
{
    // Maps configuration key → shelter display name.
    // The API key for each shelter is stored in app settings under the config key.
    // These widget API keys are public — they're embedded in each shelter's website source.
    public static readonly IReadOnlyList<(string ConfigKey, string ShelterName)> ShelterConfigs =
    [
        ("SHELTERLUV_API_KEY_KCPETPROJECT", "KC Pet Project"),
        ("SHELTERLUV_API_KEY_GREATPLAINS",  "Great Plains SPCA"),
        ("SHELTERLUV_API_KEY_HSMO",         "Humane Society of Missouri"),
    ];

    private const string AnimalsUrl = "https://www.shelterluv.com/api/v1/animals";

    public async Task<IReadOnlyList<(string ShelterName, IReadOnlyList<Dog> Dogs)>> GetAllConfiguredSheltersAsync(CancellationToken ct)
    {
        var tasks = ShelterConfigs.Select(async cfg =>
        {
            var apiKey = configuration[cfg.ConfigKey];
            if (string.IsNullOrEmpty(apiKey))
            {
                logger.LogDebug("No API key configured for {Shelter}; skipping", cfg.ShelterName);
                return (cfg.ShelterName, (IReadOnlyList<Dog>)[]);
            }

            var dogs = await GetAllDogsAsync(apiKey, cfg.ShelterName, ct);
            logger.LogInformation("Fetched {Count} dogs from {Shelter}", dogs.Count, cfg.ShelterName);
            return (cfg.ShelterName, dogs);
        });

        return [.. await Task.WhenAll(tasks)];
    }

    private async Task<IReadOnlyList<Dog>> GetAllDogsAsync(string apiKey, string shelterName, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("ShelterLuv");
        var dogs = new List<Dog>();
        var offset = 0;

        while (true)
        {
            var url = $"{AnimalsUrl}?status_type=publishable&type=Dog&offset={offset}";

            ShelterLuvAnimalsResponse? response;
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Add("x-api-key", apiKey);
                using var httpResponse = await client.SendAsync(request, ct);
                httpResponse.EnsureSuccessStatusCode();
                response = await httpResponse.Content.ReadFromJsonAsync<ShelterLuvAnimalsResponse>(cancellationToken: ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "ShelterLuv fetch failed for {Shelter} at offset {Offset}", shelterName, offset);
                break;
            }

            if (response?.Success != 1 || response.Animals is not { Count: > 0 })
            {
                break;
            }

            foreach (var animal in response.Animals)
            {
                if (animal.Type is not "Dog") continue;
                dogs.Add(MapToDog(animal, shelterName));
            }

            if (response.HasMore != true)
            {
                break;
            }

            offset += 100;
        }

        return dogs;
    }

    private static Dog MapToDog(ShelterLuvAnimal animal, string shelterName)
    {
        var photoUrl = animal.CoverPhoto is { Length: > 0 } cover && !cover.Contains("default_")
            ? cover
            : animal.Photos?.FirstOrDefault(p => !p.Contains("default_"));

        return new Dog(
            Aid:             animal.Id ?? string.Empty,
            Shelter:         shelterName,
            Name:            animal.Name?.Trim(),
            Age:             FormatAge(animal.AgeMonths),
            Gender:          animal.Sex,
            PhotoUrl:        photoUrl,
            Breed:           animal.Breed?.Trim(),
            Color:           animal.Color?.Trim(),
            Size:            animal.Size?.Trim(),
            Weight:          null,
            AdoptionFee:     null,
            CurrentLocation: animal.CurrentLocation?.Name?.Trim(),
            ProfileUrl:      animal.ProfileLink?.Trim(),
            FirstSeen:       default,
            IntakeDate:      animal.LastIntakeUnixTime > 0
                                 ? DateTimeOffset.FromUnixTimeSeconds(animal.LastIntakeUnixTime)
                                 : null);
    }

    private static string? FormatAge(long? ageMonths)
    {
        if (ageMonths is null or < 0) return null;
        var months = (int)ageMonths;
        if (months < 12) return months == 1 ? "1 month" : $"{months} months";
        var years = months / 12;
        var rem   = months % 12;
        return rem == 0
            ? (years == 1 ? "1 year" : $"{years} years")
            : $"{years} year{(years == 1 ? "" : "s")} {rem} month{(rem == 1 ? "" : "s")}";
    }

    // ── JSON response models ─────────────────────────────────────────────────

    private sealed class ShelterLuvAnimalsResponse
    {
        [JsonPropertyName("success")]     public int?                    Success    { get; init; }
        [JsonPropertyName("has_more")]    public bool?                   HasMore    { get; init; }
        [JsonPropertyName("total_count")] public int                     TotalCount { get; init; }
        [JsonPropertyName("animals")]     public List<ShelterLuvAnimal>? Animals    { get; init; }
    }

    private sealed class ShelterLuvAnimal
    {
        [JsonPropertyName("ID")]                 public string?             Id              { get; init; }
        [JsonPropertyName("Name")]               public string?             Name            { get; init; }
        [JsonPropertyName("Type")]               public string?             Type            { get; init; }
        [JsonPropertyName("Sex")]                public string?             Sex             { get; init; }
        [JsonPropertyName("Age")]                public long?               AgeMonths       { get; init; }
        [JsonPropertyName("Breed")]              public string?             Breed           { get; init; }
        [JsonPropertyName("Color")]              public string?             Color           { get; init; }
        [JsonPropertyName("Size")]               public string?             Size            { get; init; }
        [JsonPropertyName("CoverPhoto")]         public string?             CoverPhoto      { get; init; }
        [JsonPropertyName("Photos")]             public List<string>?       Photos          { get; init; }
        [JsonPropertyName("ProfileLink")]        public string?             ProfileLink     { get; init; }
        [JsonPropertyName("LastIntakeUnixTime")] public long                LastIntakeUnixTime { get; init; }
        [JsonPropertyName("CurrentLocation")]    public ShelterLuvLocation? CurrentLocation { get; init; }
    }

    private sealed class ShelterLuvLocation
    {
        [JsonPropertyName("Name")] public string? Name { get; init; }
    }
}
