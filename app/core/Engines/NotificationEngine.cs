using System.Text.Json;

using Microsoft.Extensions.Configuration;

using WebPush;

using Core.DomainModels;

namespace Core.Engines;

public sealed class NotificationEngine(IConfiguration configuration)
{
    public NotificationPayload BuildPayload(Dog dog)
    {
        var bodyParts = new List<string>();

        if (dog.Breed is not null)
        {
            bodyParts.Add($"Breed: {dog.Breed}");
        }

        if (dog.Gender is not null)
        {
            bodyParts.Add(dog.Gender);
        }

        if (dog.Age is not null)
        {
            bodyParts.Add(dog.Age);
        }

        var body = String.Join(" - ", bodyParts);

        return new NotificationPayload(
            $"{dog.Name} is available for adoption!",
            body,
            dog.PhotoUrl,
            $"/dogs/{dog.Aid}/details");
    }

    public async Task SendAsync(
        NotificationPayload payload,
        DomainModels.PushSubscription subscription,
        CancellationToken ct)
    {
        var vapidSubject = configuration["VAPID_SUBJECT"]!;
        var vapidPublicKey = configuration["VAPID_PUBLIC_KEY"]!;
        var vapidPrivateKey = configuration["VAPID_PRIVATE_KEY"]!;

        var pushSubscription = new WebPush.PushSubscription(
            subscription.Endpoint,
            subscription.P256dh,
            subscription.Auth);

        var vapidDetails = new VapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);

        var json = JsonSerializer.Serialize(new
        {
            title = payload.Title,
            body = payload.Body,
            icon = payload.Icon,
            data = new { url = payload.Url }
        });

        var client = new WebPushClient();
        await client.SendNotificationAsync(pushSubscription, json, vapidDetails, ct);
    }
}
