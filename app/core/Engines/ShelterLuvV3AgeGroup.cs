using System.Text.Json.Serialization;

namespace Core.Engines;

internal sealed class ShelterLuvV3AgeGroup
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}
