using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Api.Dtos;
using Api.Orchestrators;

namespace Api.Functions;

public sealed class StatusFunction(StatusOrchestrator statusOrchestrator)
{
    private const int DefaultPageSize = 24;

    [Function("Status")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "status")] HttpRequest req,
        CancellationToken ct)
    {
        var sort = req.Query["sort"].FirstOrDefault() ?? "newest";
        var page = int.TryParse(req.Query["page"].FirstOrDefault(), out var p) && p >= 1 ? p : 1;
        var pageSize = int.TryParse(req.Query["pageSize"].FirstOrDefault(), out var ps) && ps >= 1 && ps <= 100 ? ps : DefaultPageSize;
        var sheltersParam = req.Query["shelters"].FirstOrDefault();
        var shelterIds = sheltersParam is not null
            ? sheltersParam.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            : [];

        var result = await statusOrchestrator.GetStatusAsync(sort, page, pageSize, shelterIds, ct);

        var dogDtos = result.Dogs
            .Select(d => new DogDto(d.Aid, d.ShelterId, d.Name, d.Age, d.Gender, d.PhotoUrl, d.Breed, d.Color, d.Size, d.Weight, d.AdoptionFee, d.CurrentLocation, d.ProfileUrl, d.FirstSeen, d.IntakeDate, d.ListingDate))
            .ToList();

        var adoptedDogDtos = result.RecentlyAdopted
            .Select(d => new AdoptedDogDto(d.Aid, d.ShelterId, d.Name, d.Age, d.Gender, d.PhotoUrl, d.Breed, d.Color, d.Size, d.Weight, d.AdoptionFee, d.CurrentLocation, d.ProfileUrl, d.FirstSeen, d.IntakeDate, d.AdoptedAt))
            .ToList();

        return new OkObjectResult(new StatusResponseDto(dogDtos, result.TotalCount, result.Page, result.PageSize, result.LastChecked, result.IsMonitoringActive, adoptedDogDtos));
    }
}
