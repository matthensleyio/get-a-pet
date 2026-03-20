using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Api.Dtos;
using Core.Repositories;

namespace Api.Functions;

public sealed class FavoritesFunction(FavoritesRepository favoritesRepository)
{
    [Function("Favorites")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "delete", Route = "favorites")] HttpRequest req,
        CancellationToken ct)
    {
        if (req.Method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            var endpoint = req.Query["endpoint"].FirstOrDefault();
            if (string.IsNullOrEmpty(endpoint))
                return new BadRequestResult();

            var favorites = await favoritesRepository.GetByEndpointAsync(endpoint, ct);
            var dtos = favorites.Select(f => new FavoriteDto(f.Aid, f.ShelterId)).ToList();
            return new OkObjectResult(dtos);
        }

        var dto = await req.ReadFromJsonAsync<FavoriteRequestDto>(ct);
        if (dto is null)
            return new BadRequestResult();

        if (req.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            await favoritesRepository.AddAsync(dto.Endpoint, dto.Aid, dto.ShelterId, ct);
        }
        else
        {
            await favoritesRepository.RemoveAsync(dto.Endpoint, dto.Aid, dto.ShelterId, ct);
        }

        return new OkResult();
    }
}
