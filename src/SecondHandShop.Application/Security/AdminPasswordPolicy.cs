namespace SecondHandShop.Application.Security;

/// <summary>
/// Minimal password rules for admin accounts (aligned with security requirements; avoid logging raw passwords).
/// </summary>
public static class AdminPasswordPolicy
{
    public const int MinimumLength = 8;

    public static void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < MinimumLength)
        {
            throw new ArgumentException(
                $"Password must be at least {MinimumLength} characters.",
                nameof(password));
        }

        var hasLetter = false;
        var hasDigit = false;
        foreach (var c in password)
        {
            if (char.IsLetter(c))
                hasLetter = true;
            else if (char.IsDigit(c))
                hasDigit = true;
        }

        if (!hasLetter || !hasDigit)
        {
            throw new ArgumentException(
                "Password must contain at least one letter and one digit.",
                nameof(password));
        }
    }
}
