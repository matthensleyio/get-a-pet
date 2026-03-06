using System.Text.Json;

using WebPush;

using Api.DomainModels;

namespace Api.Engines;

public sealed class NotificationEngine(IConfiguration configuration)
{
    public NotificationPayload BuildPayload(Dog dog)
    {
        var bodyParts = new List<string>();

        if (dog.Gender is not null)
        {
            bodyParts.Add(dog.Gender);
        }

        if (dog.Age is not null)
        {
            bodyParts.Add(dog.Age);
        }

        var body = String.Join(", ", bodyParts);

        if (dog.Breed is not null)
        {
            body += $"\nBreed: {dog.Breed}";
        }

        return new NotificationPayload(
            $"New Dog: {dog.Name}",
            body,
            dog.PhotoUrl,
            dog.ProfileUrl);
    }

    public async Task<bool> SendAsync(
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

        try
        {
            await client.SendNotificationAsync(pushSubscription, json, vapidDetails, ct);
            return true;
        }
        catch (WebPushException)
        {
            return false;
        }
    }
}
