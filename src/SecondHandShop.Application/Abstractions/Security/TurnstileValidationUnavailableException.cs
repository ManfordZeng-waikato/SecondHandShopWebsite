namespace SecondHandShop.Application.Abstractions.Security;

public sealed class TurnstileValidationUnavailableException : Exception
{
    public TurnstileValidationUnavailableException(string message)
        : base(message)
    {
    }

    public TurnstileValidationUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
