using Microsoft.AspNetCore.Mvc;

using Api.DomainModels;
using Api.Dtos;
using Api.Repositories;

namespace Api.Controllers;

[ApiController]
[Route("api/subscribe")]
public sealed class SubscribeController(SubscriptionRepository subscriptionRepository) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Subscribe(
        [FromBody] PushSubscriptionRequestDto dto,
        CancellationToken ct)
    {
        var subscription = new PushSubscription(dto.Endpoint, dto.Keys.P256dh, dto.Keys.Auth);
        await subscriptionRepository.AddAsync(subscription, ct);
        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> Unsubscribe(
        [FromBody] PushSubscriptionRequestDto dto,
        CancellationToken ct)
    {
        await subscriptionRepository.RemoveByEndpointAsync(dto.Endpoint, ct);
        return Ok();
    }
}
