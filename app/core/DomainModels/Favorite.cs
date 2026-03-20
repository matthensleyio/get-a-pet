namespace Core.DomainModels;

public sealed record Favorite(
    string SubscriptionHash,
    string Aid,
    string ShelterId);
