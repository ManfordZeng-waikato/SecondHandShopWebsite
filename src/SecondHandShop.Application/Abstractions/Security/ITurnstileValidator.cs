namespace SecondHandShop.Application.Abstractions.Security;

public interface ITurnstileValidator
{
    Task<TurnstileValidationResult> ValidateAsync(
        string token,
        string? remoteIpAddress,
        CancellationToken cancellationToken = default);
}
