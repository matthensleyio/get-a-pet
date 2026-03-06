namespace Api.DomainModels;

public sealed record Dog(
    string Aid,
    string? Name,
    string? Age,
    string? Gender,
    string? PhotoUrl,
    string? Breed,
    string? ProfileUrl,
    DateTimeOffset FirstSeen);
