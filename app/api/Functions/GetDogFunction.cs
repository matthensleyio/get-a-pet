using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Api.Dtos;
using Core.DomainModels;
using Core.Repositories;

namespace Api.Functions;

public sealed class GetDogFunction(
    DogRepository dogRepository,
    AdoptedDogRepository adoptedDogRepository)
{
    [Function("GetDog")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dogs/{aid}")] HttpRequest req,
        string aid,
        CancellationToken ct)
    {
        var dog = await dogRepository.GetByAidAsync(aid, ct);
        if (dog is not null)
        {
            return new OkObjectResult(MapToDogDto(dog));
        }

        var adoptedDog = await adoptedDogRepository.GetByAidAsync(aid, ct);
        if (adoptedDog is not null)
        {
            return new OkObjectResult(MapToAdoptedDogDto(adoptedDog));
        }

        return new NotFoundResult();
    }

    private static DogDto MapToDogDto(Dog d) =>
        new(d.Aid, d.ShelterId, d.Name, d.Age, d.Gender, d.PhotoUrl, d.Breed, d.Color, d.Size, d.Weight, d.AdoptionFee, d.CurrentLocation, d.ProfileUrl, d.FirstSeen, d.IntakeDate, d.ListingDate);

    private static AdoptedDogDto MapToAdoptedDogDto(AdoptedDog d) =>
        new(d.Aid, d.ShelterId, d.Name, d.Age, d.Gender, d.PhotoUrl, d.Breed, d.Color, d.Size, d.Weight, d.AdoptionFee, d.CurrentLocation, d.ProfileUrl, d.FirstSeen, d.IntakeDate, d.AdoptedAt);
}
