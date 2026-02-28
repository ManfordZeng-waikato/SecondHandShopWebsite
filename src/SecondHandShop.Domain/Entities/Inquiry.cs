using System.Text.RegularExpressions;
using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Domain.Entities;

public partial class Inquiry
{
    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"^[0-9+\-\s()]+$")]
    private static partial Regex PhoneRegex();

    private Inquiry()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ProductId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string? CustomerName { get; private set; }
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public EmailDeliveryStatus EmailDeliveryStatus { get; private set; } = EmailDeliveryStatus.Pending;
    public DateTime? DeliveredAt { get; private set; }
    public string? DeliveryError { get; private set; }
    public int EmailSendAttempts { get; private set; }
    public DateTime? NextRetryAt { get; private set; }

    public static Inquiry Create(
        Guid productId,
        Guid customerId,
        string? customerName,
        string? email,
        string? phoneNumber,
        string message,
        DateTime utcNow)
    {
        ValidateContact(email, phoneNumber);

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Inquiry message is required.", nameof(message));
        }

        return new Inquiry
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            CustomerId = customerId,
            CustomerName = Normalize(customerName),
            Email = Normalize(email),
            PhoneNumber = Normalize(phoneNumber),
            Message = message.Trim(),
            CreatedAt = utcNow,
            EmailDeliveryStatus = EmailDeliveryStatus.Pending,
            EmailSendAttempts = 0
        };
    }

    public void MarkEmailSent(DateTime utcNow)
    {
        EmailDeliveryStatus = EmailDeliveryStatus.Sent;
        DeliveredAt = utcNow;
        DeliveryError = null;
        NextRetryAt = null;
    }

    public void MarkEmailFailed(string error, DateTime? nextRetryAt)
    {
        EmailDeliveryStatus = EmailDeliveryStatus.Failed;
        DeliveryError = string.IsNullOrWhiteSpace(error) ? "Unknown mail delivery error." : error.Trim();
        DeliveredAt = null;
        EmailSendAttempts += 1;
        NextRetryAt = nextRetryAt;
    }

    public void RequeueForEmail(DateTime? nextRetryAt)
    {
        EmailDeliveryStatus = EmailDeliveryStatus.Pending;
        DeliveryError = null;
        NextRetryAt = nextRetryAt;
    }

    private static void ValidateContact(string? email, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("At least one contact method (email or phone) is required.");
        }

        if (!string.IsNullOrWhiteSpace(email) && !EmailRegex().IsMatch(email.Trim()))
        {
            throw new ArgumentException("Invalid email format.", nameof(email));
        }

        if (!string.IsNullOrWhiteSpace(phoneNumber) && !PhoneRegex().IsMatch(phoneNumber.Trim()))
        {
            throw new ArgumentException("Phone number can only contain digits, +, -, spaces, and parentheses.", nameof(phoneNumber));
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
