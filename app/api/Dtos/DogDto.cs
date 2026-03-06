namespace Api.Dtos;

public sealed record DogDto(
    string Aid,
    string? Name,
    string? Age,
    string? Gender,
    string? PhotoUrl,
    string? Breed,
    string? ProfileUrl,
    DateTimeOffset FirstSeen,
    DateTimeOffset? IntakeDate);
