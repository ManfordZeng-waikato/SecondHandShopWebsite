using SecondHandShop.Domain.Enums;

namespace SecondHandShop.Domain.Entities;

public class Customer
{
    private Customer()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string? Name { get; private set; }
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public CustomerStatus Status { get; private set; } = CustomerStatus.New;
    public CustomerSource PrimarySource { get; private set; } = CustomerSource.Inquiry;
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastContactAtUtc { get; private set; }
    public uint RowVersion { get; private set; }

    public static Customer Create(
        string? name,
        string? email,
        string? phoneNumber,
        CustomerSource source,
        DateTime utcNow)
    {
        ValidateContact(email, phoneNumber);

        return new Customer
        {
            Id = Guid.NewGuid(),
            Name = Normalize(name),
            Email = Normalize(email),
            PhoneNumber = Normalize(phoneNumber),
            Status = CustomerStatus.New,
            PrimarySource = source,
            Notes = null,
            CreatedAt = utcNow,
            UpdatedAt = utcNow,
            LastContactAtUtc = utcNow
        };
    }

    /// <summary>
    /// Cautious merge: fills in blank fields without overwriting existing values.
    /// </summary>
    public void MergeContact(
        string? name,
        string? email,
        string? phoneNumber,
        DateTime utcNow)
    {
        var mergedName = Name ?? Normalize(name);
        var mergedEmail = Email ?? NormalizeEmail(email);
        var mergedPhoneNumber = PhoneNumber ?? Normalize(phoneNumber);

        ValidateContact(mergedEmail, mergedPhoneNumber);

        Name = mergedName;
        Email = mergedEmail;
        PhoneNumber = mergedPhoneNumber;
        LastContactAtUtc = utcNow;
        UpdatedAt = utcNow;
    }

    public void UpdateContact(
        string? name,
        string? email,
        string? phoneNumber,
        DateTime utcNow)
    {
        ValidateContact(email, phoneNumber);
        Name = Normalize(name);
        Email = Normalize(email);
        PhoneNumber = Normalize(phoneNumber);
        LastContactAtUtc = utcNow;
        UpdatedAt = utcNow;
    }

    public void UpdateByAdmin(
        string? name,
        string? phoneNumber,
        CustomerStatus status,
        string? notes,
        DateTime utcNow)
    {
        ValidateContact(Email, phoneNumber);
        Name = Normalize(name);
        PhoneNumber = Normalize(phoneNumber);
        Status = status;
        Notes = Normalize(notes);
        UpdatedAt = utcNow;
    }

    private static void ValidateContact(string? email, string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("At least one contact method (email or phone) is required.");
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeEmail(string? value)
    {
        var normalized = Normalize(value);
        return normalized?.ToLowerInvariant();
    }
}
