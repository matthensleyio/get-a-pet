using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Api.DomainModels;
using Api.Dtos;
using Api.Orchestrators;

namespace Api.Functions;

public sealed class StatusFunction(StatusOrchestrator statusOrchestrator)
{
    [Function("Status")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status")] HttpRequest req,
        CancellationToken ct)
    {
        var result = await statusOrchestrator.GetStatusAsync(ct);

        var dogDtos = result.Dogs
            .Select(MapToDogDto)
            .ToList();

        var adoptedDogDtos = result.RecentlyAdopted
            .Select(MapToAdoptedDogDto)
            .ToList();

        return new OkObjectResult(new StatusResponseDto(dogDtos, result.LastChecked, result.IsMonitoringActive, adoptedDogDtos));
    }

    private static DogDto MapToDogDto(Dog d) =>
        new(d.Aid, d.ShelterId, d.Name, d.Age, d.Gender, d.PhotoUrl, d.Breed, d.Color, d.Size, d.Weight, d.AdoptionFee, d.CurrentLocation, d.ProfileUrl, d.FirstSeen, d.IntakeDate, d.ListingDate);

    private static AdoptedDogDto MapToAdoptedDogDto(AdoptedDog d) =>
        new(d.Aid, d.ShelterId, d.Name, d.Age, d.Gender, d.PhotoUrl, d.Breed, d.Color, d.Size, d.Weight, d.AdoptionFee, d.CurrentLocation, d.ProfileUrl, d.FirstSeen, d.IntakeDate, d.AdoptedAt);
}
