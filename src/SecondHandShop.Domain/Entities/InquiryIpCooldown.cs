namespace SecondHandShop.Domain.Entities;

public class InquiryIpCooldown
{
    private InquiryIpCooldown()
    {
    }

    public string IpAddress { get; private set; } = string.Empty;
    public DateTime BlockedUntil { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static InquiryIpCooldown Create(string ipAddress, DateTime blockedUntilUtc, DateTime updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            throw new ArgumentException("IP address is required.", nameof(ipAddress));
        }

        return new InquiryIpCooldown
        {
            IpAddress = ipAddress.Trim(),
            BlockedUntil = blockedUntilUtc,
            UpdatedAt = updatedAtUtc
        };
    }

    public void SetCooldown(DateTime blockedUntilUtc, DateTime updatedAtUtc)
    {
        BlockedUntil = blockedUntilUtc;
        UpdatedAt = updatedAtUtc;
    }
}
