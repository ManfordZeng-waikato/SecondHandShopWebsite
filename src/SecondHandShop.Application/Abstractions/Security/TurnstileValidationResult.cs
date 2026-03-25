namespace SecondHandShop.Application.Abstractions.Security;

public sealed record TurnstileValidationResult
{
    public bool IsSuccess { get; init; }

    public IReadOnlyCollection<string> ErrorCodes { get; init; } = [];

    public string? Action { get; init; }

    public string? Hostname { get; init; }

    public DateTimeOffset? ChallengeTs { get; init; }
}
