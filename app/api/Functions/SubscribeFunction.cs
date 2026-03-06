using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using Api.DomainModels;
using Api.Dtos;
using Api.Repositories;

namespace Api.Functions;

public sealed class SubscribeFunction(SubscriptionRepository subscriptionRepository)
{
    [Function("Subscribe")]
    public async Task<IActionResult> RunAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "delete", Route = "subscribe")] HttpRequest req,
        CancellationToken ct)
    {
        var dto = await req.ReadFromJsonAsync<PushSubscriptionRequestDto>(ct);

        if (dto is null)
        {
            return new BadRequestResult();
        }

        if (req.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            var subscription = new PushSubscription(dto.Endpoint, dto.Keys.P256dh, dto.Keys.Auth);
            await subscriptionRepository.AddAsync(subscription, ct);
        }
        else
        {
            await subscriptionRepository.RemoveByEndpointAsync(dto.Endpoint, ct);
        }

        return new OkResult();
    }
}
