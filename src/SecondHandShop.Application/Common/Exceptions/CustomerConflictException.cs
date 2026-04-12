namespace SecondHandShop.Application.Common.Exceptions;

/// <summary>
/// Raised when an admin tries to manually create a customer whose email or phone
/// already belongs to another customer. Carries the existing customer id so the
/// caller can redirect the admin to that record instead of creating a duplicate.
/// </summary>
public sealed class CustomerConflictException : ConflictException
{
    public CustomerConflictException(
        Guid existingCustomerId,
        string conflictField,
        string message)
        : base(message)
    {
        ExistingCustomerId = existingCustomerId;
        ConflictField = conflictField;
    }

    public Guid ExistingCustomerId { get; }

    /// <summary>
    /// Either "email" or "phoneNumber".
    /// </summary>
    public string ConflictField { get; }
}
