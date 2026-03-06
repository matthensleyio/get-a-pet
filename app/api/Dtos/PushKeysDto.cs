namespace Api.Dtos;

public sealed record PushKeysDto(
    string P256dh,
    string Auth);
