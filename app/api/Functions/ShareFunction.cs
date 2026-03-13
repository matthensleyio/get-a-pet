using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Api.Repositories;
using Api.DomainModels;

namespace Api.Functions;

public sealed class ShareFunction(
    DogRepository dogRepository,
    AdoptedDogRepository adoptedDogRepository,
    IReadOnlyList<ShelterConfig> shelters,
    ILogger<ShareFunction> logger)
{
    [Function("Share")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "share/{aid}")] HttpRequest req,
        [FromRoute] string aid,
        CancellationToken ct)
    {
        logger.LogInformation("Processing share request for aid: {Aid}", aid);

        var allDogs = await dogRepository.GetAllDogsAsync(ct);
        var dog = allDogs.FirstOrDefault(d => d.Aid == aid);

        string? name = null;
        string? photoUrl = null;
        string? shelterName = null;

        if (dog != null)
        {
            name = dog.Name;
            photoUrl = dog.PhotoUrl;
            shelterName = shelters.FirstOrDefault(s => s.ShelterId == dog.ShelterId)?.ShelterName ?? dog.ShelterId;
        }
        else
        {
            var recentAdopted = await adoptedDogRepository.GetRecentAsync(ct);
            var adoptedDog = recentAdopted.FirstOrDefault(d => d.Aid == aid);
            if (adoptedDog != null)
            {
                name = adoptedDog.Name;
                photoUrl = adoptedDog.PhotoUrl;
                shelterName = shelters.FirstOrDefault(s => s.ShelterId == adoptedDog.ShelterId)?.ShelterName ?? adoptedDog.ShelterId;
            }
        }

        var baseUrl = $"{req.Scheme}://{req.Host}";
        string title = "Get a Pet";
        string description = "The fastest way from shelter to sofa.";
        string image = $"{baseUrl}/icon-512.png";

        if (name != null)
        {
            title = $"Meet {name}";
            description = $"@ {shelterName}";
            if (!string.IsNullOrEmpty(photoUrl))
            {
                image = photoUrl;
            }
        }

        // Security: Encode all values for HTML/JS context
        var safeAid = WebUtility.HtmlEncode(aid);
        var safeTitle = WebUtility.HtmlEncode(title);
        var safeDescription = WebUtility.HtmlEncode(description);
        var safeImage = WebUtility.HtmlEncode(image);
        var safeRedirectUrl = $"/dogs/{WebUtility.UrlEncode(aid)}/details";

        string html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{safeTitle}</title>
    <meta name=""title"" content=""{safeTitle}"">
    <meta name=""description"" content=""{safeDescription}"">
    <meta property=""og:type"" content=""website"">
    <meta property=""og:title"" content=""{safeTitle}"">
    <meta property=""og:description"" content=""{safeDescription}"">
    <meta property=""og:image"" content=""{safeImage}"">
    <meta property=""twitter:card"" content=""summary_large_image"">
    <meta property=""twitter:title"" content=""{safeTitle}"">
    <meta property=""twitter:description"" content=""{safeDescription}"">
    <meta property=""twitter:image"" content=""{safeImage}"">
    <script>window.location.href = '{safeRedirectUrl}';</script>
</head>
<body>
    Redirecting to <a href=""{safeRedirectUrl}"">{safeTitle}</a>...
</body>
</html>";

        return new ContentResult
        {
            Content = html,
            ContentType = "text/html",
            StatusCode = (int)HttpStatusCode.OK
        };
    }
}
