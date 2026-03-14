namespace Core.DomainModels;

public sealed record Dog(
    string Aid,
    string ShelterId,
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
    DateTimeOffset? ListingDate);
