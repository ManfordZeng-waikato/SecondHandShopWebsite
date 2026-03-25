using System.Text.Json.Serialization;

namespace SecondHandShop.Infrastructure.Services;

public sealed record TurnstileSiteVerifyResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("error-codes")]
    public IReadOnlyCollection<string> ErrorCodes { get; init; } = [];

    [JsonPropertyName("challenge_ts")]
    public DateTimeOffset? ChallengeTs { get; init; }

    [JsonPropertyName("hostname")]
    public string? Hostname { get; init; }

    [JsonPropertyName("action")]
    public string? Action { get; init; }

    [JsonPropertyName("cdata")]
    public string? CData { get; init; }
}
