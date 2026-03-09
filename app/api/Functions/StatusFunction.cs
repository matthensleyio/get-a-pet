using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

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
            .Select(d => new DogDto(d.Aid, d.Name, d.Age, d.Gender, d.PhotoUrl, d.Breed, d.Color, d.Size, d.Weight, d.AdoptionFee, d.CurrentLocation, d.ProfileUrl, d.FirstSeen, d.IntakeDate, d.ListingDate))
            .ToList();

        var adoptedDogDtos = result.RecentlyAdopted
            .Select(d => new AdoptedDogDto(d.Aid, d.Name, d.Age, d.Gender, d.PhotoUrl, d.Breed, d.Color, d.Size, d.Weight, d.AdoptionFee, d.CurrentLocation, d.ProfileUrl, d.FirstSeen, d.IntakeDate, d.AdoptedAt))
            .ToList();

        return new OkObjectResult(new StatusResponseDto(dogDtos, result.Count, result.LastChecked, result.IsMonitoringActive, adoptedDogDtos));
    }
}
