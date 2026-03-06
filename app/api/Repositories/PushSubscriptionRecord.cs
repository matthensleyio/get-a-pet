namespace Api.Repositories;

internal sealed class PushSubscriptionRecord
{
    public string EndpointHash { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string P256dh { get; set; } = "";
    public string Auth { get; set; } = "";
}
