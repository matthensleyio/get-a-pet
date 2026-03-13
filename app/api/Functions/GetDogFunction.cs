using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Api.Dtos;
using Api.Orchestrators;

namespace Api.Functions;

public sealed class GetDogFunction(StatusOrchestrator statusOrchestrator)
{
    [Function("GetDog")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "dogs/{aid}")] HttpRequest req,
        string aid,
        CancellationToken ct)
    {
        var (dog, adoptedDog) = await statusOrchestrator.GetDogAsync(aid, ct);

        if (dog is not null)
        {
            return new OkObjectResult(new DogDto(
                dog.Aid, dog.ShelterId, dog.Name, dog.Age, dog.Gender, dog.PhotoUrl,
                dog.Breed, dog.Color, dog.Size, dog.Weight, dog.AdoptionFee,
                dog.CurrentLocation, dog.ProfileUrl, dog.FirstSeen, dog.IntakeDate, dog.ListingDate));
        }

        if (adoptedDog is not null)
        {
            return new OkObjectResult(new AdoptedDogDto(
                adoptedDog.Aid, adoptedDog.ShelterId, adoptedDog.Name, adoptedDog.Age, adoptedDog.Gender, adoptedDog.PhotoUrl,
                adoptedDog.Breed, adoptedDog.Color, adoptedDog.Size, adoptedDog.Weight, adoptedDog.AdoptionFee,
                adoptedDog.CurrentLocation, adoptedDog.ProfileUrl, adoptedDog.FirstSeen, adoptedDog.IntakeDate, adoptedDog.AdoptedAt));
        }

        return new NotFoundResult();
    }
}
