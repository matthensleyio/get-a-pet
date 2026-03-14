namespace Core.DomainModels;

public sealed record NotificationPayload(
    string Title,
    string Body,
    string? Icon,
    string? Url);
