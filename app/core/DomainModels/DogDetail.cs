namespace Core.DomainModels;

public sealed record DogDetail(
    string? Breed,
    string? Color,
    string? Size,
    string? Weight,
    string? AdoptionFee,
    string? CurrentLocation,
    DateTimeOffset? IntakeDate,
    IReadOnlyList<string>? PhotoUrls = null);
