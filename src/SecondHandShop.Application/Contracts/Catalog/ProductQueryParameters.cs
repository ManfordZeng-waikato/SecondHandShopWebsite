using System.Text.RegularExpressions;

namespace SecondHandShop.Application.Contracts.Catalog;

public sealed partial record ProductQueryParameters
{
    public const int MaxSearchLength = 100;
    public const int MaxCategoryLength = 100;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 24;
    public string? Category { get; init; }
    public string? Search { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? Status { get; init; }
    public string? Sort { get; init; }

    public int SafePage => Math.Max(1, Page);
    public int SafePageSize => Math.Clamp(PageSize, 1, 100);
    public string? SafeSearch => Sanitize(Search, MaxSearchLength);
    public string? SafeCategory => Sanitize(Category, MaxCategoryLength);

    private static string? Sanitize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var cleaned = ControlCharRegex().Replace(value, "").Trim();
        if (cleaned.Length == 0)
            return null;

        return cleaned.Length > maxLength ? cleaned[..maxLength] : cleaned;
    }

    [GeneratedRegex(@"[\x00-\x1F\x7F]")]
    private static partial Regex ControlCharRegex();
}
