using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Engines;

internal sealed class ShelterLuvV3Animal
{
    [JsonPropertyName("uniqueId")]
    public string UniqueId { get; init; } = "";

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("sex")]
    public string? Sex { get; init; }

    [JsonPropertyName("age_group")]
    public ShelterLuvV3AgeGroup? AgeGroup { get; init; }

    [JsonPropertyName("weight_group")]
    public string? WeightGroup { get; init; }

    [JsonPropertyName("breed")]
    public string? Breed { get; init; }

    [JsonPropertyName("secondary_breed")]
    public string? SecondaryBreed { get; init; }

    [JsonPropertyName("primary_color")]
    public string? PrimaryColor { get; init; }

    [JsonPropertyName("secondary_color")]
    public string? SecondaryColor { get; init; }

    [JsonPropertyName("intake_date")]
    public string? IntakeDate { get; init; }

    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("photos")]
    public JsonElement Photos { get; init; }

    [JsonPropertyName("public_url")]
    public string? PublicUrl { get; init; }
}
