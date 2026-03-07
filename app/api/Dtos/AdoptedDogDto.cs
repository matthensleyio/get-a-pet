namespace Api.Dtos;

public sealed record AdoptedDogDto(
    string Aid,
    string? Name,
    string? Age,
    string? Gender,
    string? PhotoUrl,
    string? Breed,
    string? Color,
    string? Size,
    string? Weight,
    string? AdoptionFee,
    string? CurrentLocation,
    string? ProfileUrl,
    DateTimeOffset FirstSeen,
    DateTimeOffset? IntakeDate,
    DateTimeOffset AdoptedAt);
