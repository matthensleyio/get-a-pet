namespace Api.Dtos;

public sealed record PushSubscriptionRequestDto(
    string Endpoint,
    PushKeysDto Keys);
