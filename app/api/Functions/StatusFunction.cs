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
            .Select(d => new DogDto(d.Aid, d.Name, d.Age, d.Gender, d.PhotoUrl, d.Breed, d.ProfileUrl, d.FirstSeen))
            .ToList();

        return new OkObjectResult(new StatusResponseDto(dogDtos, result.Count, result.LastChecked, result.IsMonitoringActive));
    }
}
