namespace Api.Dtos;

public sealed record StatusResponseDto(
    IReadOnlyList<DogDto> Dogs,
    int TotalCount,
    int Page,
    int PageSize,
    DateTimeOffset? LastChecked,
    bool IsMonitoringActive,
    IReadOnlyList<AdoptedDogDto> RecentlyAdopted);
