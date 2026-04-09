namespace SecondHandShop.Domain.Common;

/// <summary>
/// The request is well-formed and not in conflict, but violates a business/domain rule
/// (e.g. trying to feature an unavailable product, exceeding a per-entity limit).
/// Mapped to HTTP 422 Unprocessable Entity by the WebApi exception filter.
/// </summary>
public sealed class DomainRuleViolationException : Exception
{
    public DomainRuleViolationException(string message) : base(message) { }
    public DomainRuleViolationException(string message, Exception innerException) : base(message, innerException) { }
}
