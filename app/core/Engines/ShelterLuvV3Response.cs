using System.Text.Json.Serialization;

namespace Core.Engines;

internal sealed class ShelterLuvV3Response
{
    [JsonPropertyName("animals")]
    public IReadOnlyList<ShelterLuvV3Animal> Animals { get; init; } = [];
}
