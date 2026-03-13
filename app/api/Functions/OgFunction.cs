using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Api.DomainModels;
using Api.Repositories;

namespace Api.Functions;

public sealed class OgFunction(DogRepository dogRepository, IReadOnlyList<ShelterConfig> shelters)
{
    [Function("OgPreview")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "share/{aid}")] HttpRequest req,
        string aid,
        CancellationToken ct)
    {
        var dogs = await dogRepository.GetAllDogsAsync(ct);
        var dog = dogs.FirstOrDefault(d => d.Aid == aid);

        if (dog is null)
            return new RedirectResult("/");

        var shelterName = shelters.FirstOrDefault(s => s.ShelterId == dog.ShelterId)?.ShelterName ?? dog.ShelterId;
        var name = dog.Name ?? "A Dog";
        var appUrl = $"{req.Scheme}://{req.Host}";
        var encodedAid = Uri.EscapeDataString(aid);
        var detailPath = $"/dogs/{encodedAid}/details";
        var photoUrl = dog.PhotoUrl ?? $"{appUrl}/icon-512.png";

        var title = WebUtility.HtmlEncode($"Meet {name}");
        var description = WebUtility.HtmlEncode($"@ {shelterName}");
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
              <title>{title} - Get a Pet</title>
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
