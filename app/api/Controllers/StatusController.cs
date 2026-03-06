using Microsoft.AspNetCore.Mvc;

using Api.Dtos;
using Api.Orchestrators;

namespace Api.Controllers;

[ApiController]
[Route("api/status")]
public sealed class StatusController(StatusOrchestrator statusOrchestrator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StatusResponseDto>> GetStatus(CancellationToken ct)
    {
        var result = await statusOrchestrator.GetStatusAsync(ct);

        var dogDtos = result.Dogs
            .Select(d => new DogDto(d.Aid, d.Name, d.Age, d.Gender, d.PhotoUrl, d.Breed, d.ProfileUrl))
            .ToList();

        return Ok(new StatusResponseDto(dogDtos, result.Count, result.LastChecked, result.IsMonitoringActive));
    }
}
