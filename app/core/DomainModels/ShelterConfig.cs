namespace Core.DomainModels;

public sealed record ShelterConfig(
    string ShelterId,
    string ShelterName,
    int PetBridgeClientId,
    string ProfileUrlTemplate,
    string IntakeDateLabel);
