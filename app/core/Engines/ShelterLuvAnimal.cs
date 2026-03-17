using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Engines;

internal sealed class ShelterLuvAnimal
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("gender")]
    public string? Gender { get; init; }

    [JsonPropertyName("age")]
    public string? Age { get; init; }

    [JsonPropertyName("size")]
    public string? Size { get; init; }

    [JsonPropertyName("price")]
    public JsonElement Price { get; init; }

    [JsonPropertyName("primarybreed")]
    public string? PrimaryBreed { get; init; }

    [JsonPropertyName("secondarybreed")]
    public string? SecondaryBreed { get; init; }

    [JsonPropertyName("primarycolor")]
    public string? PrimaryColor { get; init; }

    [JsonPropertyName("secondarycolor")]
    public string? SecondaryColor { get; init; }

    [JsonPropertyName("intakeDate")]
    public long IntakeDate { get; init; }

    [JsonPropertyName("cover")]
    public string? Cover { get; init; }
}
