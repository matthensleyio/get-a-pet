using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Api.DomainModels;

namespace Api.Engines;

public sealed class PetfinderEngine(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<PetfinderEngine> logger)
{
    // Petfinder organization IDs → shelter display names
    public static readonly IReadOnlyDictionary<string, string> OrgShelterMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["MO179"] = "Humane Society of Missouri",
            ["MO208"] = "Humane Society of Missouri",
            ["MO579"] = "KC Pet Project",
            ["KS07"]  = "Great Plains SPCA",
        };

    // Token cache (shared for the lifetime of the engine instance)
    private string? _accessToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    private const string TokenUrl   = "https://api.petfinder.com/v2/oauth2/token";
    private const string AnimalsUrl = "https://api.petfinder.com/v2/animals";

    public async Task<IReadOnlyList<Dog>> GetAllDogsAsync(string orgId, string shelterName, CancellationToken ct)
    {
        var token = await GetTokenAsync(ct);
        if (token is null)
        {
            logger.LogWarning("Petfinder token unavailable; skipping {OrgId}", orgId);
            return [];
        }

        var client = httpClientFactory.CreateClient("Petfinder");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var dogs = new List<Dog>();
        var page = 1;

        while (true)
        {
            var url = $"{AnimalsUrl}?organization={orgId}&type=Dog&status=adoptable&limit=100&page={page}";

            PetfinderAnimalsResponse? response;
            try
            {
                response = await client.GetFromJsonAsync<PetfinderAnimalsResponse>(url, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Petfinder fetch failed for org {OrgId} page {Page}", orgId, page);
                break;
            }

            if (response?.Animals is not { Count: > 0 })
            {
                break;
            }

            foreach (var animal in response.Animals)
            {
                dogs.Add(MapToDog(animal, shelterName));
            }

            if (page >= (response.Pagination?.TotalPages ?? 1))
            {
                break;
            }

            page++;
        }

        return dogs;
    }

    private async Task<string?> GetTokenAsync(CancellationToken ct)
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow < _tokenExpiry.AddMinutes(-1))
        {
            return _accessToken;
        }

        var apiKey    = configuration["PETFINDER_API_KEY"];
        var apiSecret = configuration["PETFINDER_API_SECRET"];

        if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
        {
            logger.LogWarning("PETFINDER_API_KEY or PETFINDER_API_SECRET not configured");
            return null;
        }

        var client = httpClientFactory.CreateClient("Petfinder");

        try
        {
            var tokenResponse = await client.PostAsync(TokenUrl, new FormUrlEncodedContent(
            [
                new("grant_type",    "client_credentials"),
                new("client_id",     apiKey),
                new("client_secret", apiSecret),
            ]), ct);

            tokenResponse.EnsureSuccessStatusCode();

            var json = await tokenResponse.Content.ReadFromJsonAsync<PetfinderTokenResponse>(cancellationToken: ct);
            _accessToken = json?.AccessToken;
            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(json?.ExpiresIn ?? 3600);

            return _accessToken;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to obtain Petfinder access token");
            return null;
        }
    }

    private static Dog MapToDog(PetfinderAnimal animal, string shelterName)
    {
        var breed = BuildBreedString(animal.Breeds);
        var photoUrl = animal.Photos?.FirstOrDefault()?.Large
                    ?? animal.Photos?.FirstOrDefault()?.Medium
                    ?? animal.Photos?.FirstOrDefault()?.Small;

        return new Dog(
            Aid:             animal.Id.ToString(),
            Shelter:         shelterName,
            Name:            animal.Name?.Trim(),
            Age:             animal.Age,
            Gender:          animal.Gender,
            PhotoUrl:        photoUrl,
            Breed:           breed,
            Color:           animal.Colors?.Primary,
            Size:            animal.Size,
            Weight:          null,
            AdoptionFee:     null,
            CurrentLocation: null,
            ProfileUrl:      animal.Url,
            FirstSeen:       default,
            IntakeDate:      animal.PublishedAt);
    }

    private static string? BuildBreedString(PetfinderBreeds? breeds)
    {
        if (breeds is null) return null;

        var primary = breeds.Primary?.Trim();
        if (string.IsNullOrEmpty(primary)) return null;

        if (!string.IsNullOrWhiteSpace(breeds.Secondary))
        {
            return $"{primary} / {breeds.Secondary.Trim()}";
        }

        if (breeds.Mixed == true && !primary.Contains("Mix", StringComparison.OrdinalIgnoreCase))
        {
            return $"{primary} Mix";
        }

        return primary;
    }

    // ── JSON response models ─────────────────────────────────────────────────

    private sealed class PetfinderTokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; init; }
        [JsonPropertyName("expires_in")]   public int ExpiresIn { get; init; }
    }

    private sealed class PetfinderAnimalsResponse
    {
        [JsonPropertyName("animals")]    public List<PetfinderAnimal>?    Animals    { get; init; }
        [JsonPropertyName("pagination")] public PetfinderPagination?      Pagination { get; init; }
    }

    private sealed class PetfinderPagination
    {
        [JsonPropertyName("total_pages")] public int TotalPages { get; init; }
    }

    private sealed class PetfinderAnimal
    {
        [JsonPropertyName("id")]           public long            Id           { get; init; }
        [JsonPropertyName("name")]         public string?         Name         { get; init; }
        [JsonPropertyName("age")]          public string?         Age          { get; init; }
        [JsonPropertyName("gender")]       public string?         Gender       { get; init; }
        [JsonPropertyName("size")]         public string?         Size         { get; init; }
        [JsonPropertyName("url")]          public string?         Url          { get; init; }
        [JsonPropertyName("published_at")] public DateTimeOffset? PublishedAt  { get; init; }
        [JsonPropertyName("breeds")]       public PetfinderBreeds? Breeds      { get; init; }
        [JsonPropertyName("colors")]       public PetfinderColors? Colors      { get; init; }
        [JsonPropertyName("photos")]       public List<PetfinderPhoto>? Photos { get; init; }
    }

    private sealed class PetfinderBreeds
    {
        [JsonPropertyName("primary")]   public string? Primary   { get; init; }
        [JsonPropertyName("secondary")] public string? Secondary { get; init; }
        [JsonPropertyName("mixed")]     public bool?   Mixed     { get; init; }
    }

    private sealed class PetfinderColors
    {
        [JsonPropertyName("primary")] public string? Primary { get; init; }
    }

    private sealed class PetfinderPhoto
    {
        [JsonPropertyName("small")]  public string? Small  { get; init; }
        [JsonPropertyName("medium")] public string? Medium { get; init; }
        [JsonPropertyName("large")]  public string? Large  { get; init; }
    }
}
