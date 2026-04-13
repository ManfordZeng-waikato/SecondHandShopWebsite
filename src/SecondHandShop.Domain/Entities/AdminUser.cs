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
    /// <summary>
    /// When true, the admin must change password before using full back-office APIs (enforced via JWT claim + authorization policy).
    /// </summary>
    public bool MustChangePassword { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Monotonic counter embedded as a JWT claim. Any server-side event that should invalidate
    /// existing tokens globally (password change, forced logout, credential reset) bumps this
    /// via <see cref="BumpTokenVersion"/>. The JWT pipeline compares the claim against the DB
    /// value on every authenticated request and rejects stale tokens immediately.
    /// </summary>
    public int TokenVersion { get; private set; }

    public int FailedLoginCount { get; private set; }

    /// <summary>
    /// Until this instant (UTC) login is blocked regardless of password correctness.
    /// Null means not locked.
    /// </summary>
    public DateTime? LockedUntilUtc { get; private set; }

    public DateTime? LastSuccessfulLoginAtUtc { get; private set; }

    public string? LastSuccessfulLoginIp { get; private set; }

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
        string role = "Admin",
        bool mustChangePassword = false)
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
            MustChangePassword = mustChangePassword,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Updates password after verifying the current secret out-of-band; clears forced-change flag
    /// and bumps TokenVersion so that any previously-issued JWT (including the restricted one used
    /// to hit this endpoint) is rejected on its next request.
    /// </summary>
    public void CompleteForcedPasswordChange(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new ArgumentException("Password hash is required.", nameof(newPasswordHash));

        PasswordHash = newPasswordHash;
        MustChangePassword = false;
        TokenVersion++;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }

    public bool IsLockedOut(DateTime utcNow)
        => LockedUntilUtc is { } until && until > utcNow;

    /// <summary>
    /// Records a failed login attempt. After <paramref name="maxAttempts"/> consecutive failures
    /// the account is locked for <paramref name="lockoutDuration"/>. The counter keeps growing
    /// past the lock threshold so repeated attacks extend the lock on each attempt.
    /// </summary>
    public void RegisterFailedLogin(int maxAttempts, TimeSpan lockoutDuration, DateTime utcNow)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= maxAttempts)
        {
            LockedUntilUtc = utcNow + lockoutDuration;
        }
    }

    public void RegisterSuccessfulLogin(DateTime utcNow, string? sourceIpAddress)
    {
        FailedLoginCount = 0;
        LockedUntilUtc = null;
        LastSuccessfulLoginAtUtc = utcNow;
        LastSuccessfulLoginIp = Truncate(sourceIpAddress, 64);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
