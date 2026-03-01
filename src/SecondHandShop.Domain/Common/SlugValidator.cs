using System.Text.RegularExpressions;

namespace SecondHandShop.Domain.Common;

public static partial class SlugValidator
{
    [GeneratedRegex(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled)]
    private static partial Regex SlugPattern();

    public static bool IsValid(string slug)
    {
        return !string.IsNullOrWhiteSpace(slug) && SlugPattern().IsMatch(slug);
    }

    public static void EnsureValid(string slug, string paramName)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }

        if (!SlugPattern().IsMatch(slug.Trim().ToLowerInvariant()))
        {
            throw new ArgumentException(
                $"{paramName} must contain only lowercase letters, numbers, and hyphens (e.g. 'vintage-leather-bag').",
                paramName);
        }
    }
}
