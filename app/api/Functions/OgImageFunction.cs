using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

using Api.Repositories;

namespace Api.Functions;

public sealed class OgImageFunction(DogRepository dogRepository, IHttpClientFactory httpClientFactory)
{
    private static readonly Rgba32 Background = new(0xFF, 0xF8, 0xEE);
    private const int CanvasWidth = 1200;
    private const int CanvasHeight = 630;
    private const int Padding = 48;

    [Function("OgImage")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "share/image/{aid}")] HttpRequest req,
        string aid,
        CancellationToken ct)
    {
        var dogs = await dogRepository.GetAllDogsAsync(ct);
        var dog = dogs.FirstOrDefault(d => d.Aid == aid);

        if (dog?.PhotoUrl is null)
            return new RedirectResult("/icon-512.png");

        byte[] photoBytes;
        try
        {
            var client = httpClientFactory.CreateClient("PetBridge");
            photoBytes = await client.GetByteArrayAsync(dog.PhotoUrl, ct);
        }
        catch
        {
            return new RedirectResult("/icon-512.png");
        }

        using var petImage = await Image.LoadAsync<Rgba32>(new MemoryStream(photoBytes), ct);

        var maxW = CanvasWidth - Padding * 2;
        var maxH = CanvasHeight - Padding * 2;
        var scale = Math.Min((float)maxW / petImage.Width, (float)maxH / petImage.Height);
        var newW = (int)(petImage.Width * scale);
        var newH = (int)(petImage.Height * scale);
        petImage.Mutate(ctx => ctx.Resize(newW, newH));

        using var canvas = new Image<Rgba32>(CanvasWidth, CanvasHeight, Background);
        canvas.Mutate(ctx => ctx.DrawImage(petImage, new Point((CanvasWidth - newW) / 2, (CanvasHeight - newH) / 2), 1f));

        var ms = new MemoryStream();
        await canvas.SaveAsJpegAsync(ms, new JpegEncoder { Quality = 85 }, ct);

        req.HttpContext.Response.Headers["Cache-Control"] = "public, max-age=3600";
        return new FileContentResult(ms.ToArray(), "image/jpeg");
    }
}
