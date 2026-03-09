namespace Api.DomainModels;

public sealed record PushSubscription(
    string Endpoint,
    string P256dh,
    string Auth,
    IReadOnlyList<string> ShelterIds);
