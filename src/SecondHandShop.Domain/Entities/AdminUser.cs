namespace SecondHandShop.Domain.Entities;

public class AdminUser
{
    private AdminUser()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public static AdminUser Create(string displayName, string email)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        return new AdminUser
        {
            Id = Guid.NewGuid(),
            DisplayName = displayName.Trim(),
            Email = email.Trim()
        };
    }

    public static AdminUser CreateWithCredentials(
        string userName,
        string displayName,
        string passwordHash,
        string role = "Admin")
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("User name is required.", nameof(userName));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name is required.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));

        return new AdminUser
        {
            Id = Guid.NewGuid(),
            UserName = userName.Trim(),
            DisplayName = displayName.Trim(),
            Email = $"{userName.Trim()}@admin.local",
            PasswordHash = passwordHash,
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
