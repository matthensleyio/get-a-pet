namespace Api.Repositories;

internal sealed class DogRecord
{
    public string Aid { get; set; } = "";
    public string? Name { get; set; }
    public string? Age { get; set; }
    public string? Gender { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Breed { get; set; }
    public string? ProfileUrl { get; set; }
    public DateTimeOffset FirstSeen { get; set; }
}
