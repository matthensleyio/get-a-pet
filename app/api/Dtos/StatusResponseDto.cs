namespace Api.Dtos;

public sealed record StatusResponseDto(
    IReadOnlyList<DogDto> Dogs,
    int Count,
    DateTimeOffset? LastChecked,
    bool IsMonitoringActive,
    IReadOnlyList<AdoptedDogDto> RecentlyAdopted);
