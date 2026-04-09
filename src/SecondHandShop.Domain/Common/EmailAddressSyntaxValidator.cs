using System.Net.Mail;

namespace SecondHandShop.Domain.Common;

/// <summary>
/// Shared email syntax checks (inquiry, customer, API models). Stricter than a single-dot TLD (e.g. rejects <c>a@b.c</c>).
/// </summary>
public static class EmailAddressSyntaxValidator
{
    private const int MaxEmailLength = 256;

    /// <summary>
    /// Returns true when <paramref name="email"/> is non-empty and structurally valid for typical SMTP use.
    /// </summary>
    public static bool IsValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        email = email.Trim();
        if (email.Length > MaxEmailLength)
        {
            return false;
        }

        try
        {
            var parsed = new MailAddress(email);
            var host = parsed.Host;
            if (string.IsNullOrEmpty(host))
            {
                return false;
            }

            var lastDot = host.LastIndexOf('.');
            if (lastDot <= 0 || lastDot >= host.Length - 1)
            {
                return false;
            }

            var tld = host[(lastDot + 1)..];
            if (tld.Length < 2)
            {
                return false;
            }

            foreach (var c in tld)
            {
                if (c != '-' && !char.IsAsciiLetter(c))
                {
                    return false;
                }
            }

            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
