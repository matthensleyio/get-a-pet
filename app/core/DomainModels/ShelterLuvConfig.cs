namespace Core.DomainModels;

public sealed record ShelterLuvConfig(
    string ShelterId,
    string ShelterName,
    string ApiUrl,
    string ProfileUrlTemplate);
