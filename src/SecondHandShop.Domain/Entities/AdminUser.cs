namespace SecondHandShop.Domain.Entities;

public class AdminUser
{
    private AdminUser()
    {
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public string DisplayName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

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

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}
