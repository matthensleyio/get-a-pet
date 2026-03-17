using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Core.DomainModels;
using Api.Dtos;

namespace Api.Functions;

public sealed class SheltersFunction(
    IReadOnlyList<ShelterConfig> shelters,
    IReadOnlyList<ShelterLuvConfig> shelterLuvShelters)
{
    [Function("Shelters")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shelters")] HttpRequest req)
    {
        var dtos = shelters
            .Select(s => new ShelterDto(s.ShelterId, s.ShelterName))
            .Concat(shelterLuvShelters.Select(s => new ShelterDto(s.ShelterId, s.ShelterName)))
            .ToArray();

        return new OkObjectResult(dtos);
    }
}
