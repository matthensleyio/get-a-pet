namespace Api.Dtos;

public sealed record StatusResponseDto(
    IReadOnlyList<DogDto> Dogs,
    DateTimeOffset? LastChecked,
    bool IsMonitoringActive,
    IReadOnlyList<AdoptedDogDto> RecentlyAdopted);
