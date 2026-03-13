using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Api.DomainModels;
using Api.Orchestrators;
using Api.Repositories;

namespace Api.Functions;

public sealed class OgFunction(StatusOrchestrator statusOrchestrator, IReadOnlyList<ShelterConfig> shelters)
{
    [Function("OgPreview")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "share/{aid}")] HttpRequest req,
        string aid,
        CancellationToken ct)
    {
        var (dog, adoptedDog) = await statusOrchestrator.GetDogAsync(aid, ct);
        var anyDog = (object?)dog ?? adoptedDog;

        if (anyDog is null)
            return new RedirectResult("/");

        var dogAid = dog?.Aid ?? adoptedDog!.Aid;
        var dogShelterId = dog?.ShelterId ?? adoptedDog!.ShelterId;
        var dogName = dog?.Name ?? adoptedDog!.Name;
        var dogAge = dog?.Age ?? adoptedDog!.Age;
        var dogBreed = dog?.Breed ?? adoptedDog!.Breed;
        var dogPhotoUrl = dog?.PhotoUrl ?? adoptedDog!.PhotoUrl;

        var shelterName = shelters.FirstOrDefault(s => s.ShelterId == dogShelterId)?.ShelterName ?? dogShelterId;
        var name = dogName ?? "A Dog";
        var appUrl = $"{req.Scheme}://{req.Host}";
        var encodedAid = Uri.EscapeDataString(dogAid);
        var detailPath = $"/dogs/{encodedAid}/details";
        var photoUrl = dogPhotoUrl ?? $"{appUrl}/icon-512.png";

        var ageNum = dogAge?.Split(' ').FirstOrDefault(t => int.TryParse(t, out _));
        string descriptionText;
        if (ageNum is not null && dogBreed is not null)
            descriptionText = $"They're a {ageNum} year old {dogBreed} at {shelterName}";
        else if (ageNum is not null)
            descriptionText = $"They're {ageNum} years old at {shelterName}";
        else if (dogBreed is not null)
            descriptionText = $"They're a {dogBreed} at {shelterName}";
        else
            descriptionText = $"Available at {shelterName}";

        var title = WebUtility.HtmlEncode($"Someone thinks you would love to meet {name}");
        var description = WebUtility.HtmlEncode(descriptionText);
        var encodedImageUrl = WebUtility.HtmlEncode(photoUrl);
        var encodedDetailUrl = WebUtility.HtmlEncode($"{appUrl}{detailPath}");

        var html = $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8">
              <meta property="og:type" content="website">
              <meta property="og:url" content="{encodedDetailUrl}">
              <meta property="og:title" content="{title}">
              <meta property="og:description" content="{description}">
              <meta property="og:image" content="{encodedImageUrl}">
              <meta name="twitter:card" content="summary">
              <meta name="twitter:title" content="{title}">
              <meta name="twitter:description" content="{description}">
              <meta name="twitter:image" content="{encodedImageUrl}">
              <title>{dogName} - Get a Pet</title>
              <meta http-equiv="refresh" content="0; url={detailPath}">
            </head>
            <body>
              <script>window.location.replace('{detailPath}');</script>
            </body>
            </html>
            """;

        return new ContentResult
        {
            Content = html,
            ContentType = "text/html; charset=utf-8",
            StatusCode = 200
        };
    }
}
