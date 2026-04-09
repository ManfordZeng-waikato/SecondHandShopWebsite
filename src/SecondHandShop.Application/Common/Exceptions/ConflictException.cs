namespace SecondHandShop.Application.Common.Exceptions;

/// <summary>
/// The request conflicts with the current state of a resource (duplicate key, already-set invariant,
/// concurrent modification). Mapped to HTTP 409 Conflict by the WebApi exception filter.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
    public ConflictException(string message, Exception innerException) : base(message, innerException) { }
}
