namespace SecondHandShop.Application.Abstractions.Messaging;

public sealed record AdminLoginNotificationMessage(
    Guid AdminUserId,
    string UserName,
    string DisplayName,
    string Email,
    DateTime OccurredAtUtc,
    string? SourceIpAddress);
