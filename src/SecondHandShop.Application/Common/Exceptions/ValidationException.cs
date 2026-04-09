namespace SecondHandShop.Application.Common.Exceptions;

/// <summary>
/// The request is malformed or violates input-shape constraints (missing field, wrong file type,
/// path/route mismatch). Mapped to HTTP 400 Bad Request by the WebApi exception filter.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
    public ValidationException(string message, Exception innerException) : base(message, innerException) { }
}
