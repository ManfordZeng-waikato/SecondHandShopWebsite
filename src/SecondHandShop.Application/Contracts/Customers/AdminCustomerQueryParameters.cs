using System.Text.RegularExpressions;

namespace SecondHandShop.Application.Contracts.Customers;

public sealed partial record AdminCustomerQueryParameters
{
    public const int MaxSearchLength = 100;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? SortBy { get; init; } = "createdAt";
    public string? SortDirection { get; init; } = "desc";

    public int SafePage => Math.Max(1, Page);
    public int SafePageSize => Math.Clamp(PageSize, 1, 100);
    public string? SafeSearch => Sanitize(Search, MaxSearchLength);

    public string SafeSortBy
    {
        get
        {
            var normalized = SortBy?.Trim().ToLowerInvariant();
            return normalized switch
            {
                "updatedat" or "updated_at" => "updatedAt",
                "lastinquiryat" or "last_inquiry_at" => "lastInquiryAt",
                "totalspent" or "total_spent" => "totalSpent",
                "lastpurchaseat" or "last_purchase_at" => "lastPurchaseAtUtc",
                _ => "createdAt"
            };
        }
    }

    public bool IsSortDescending =>
        !string.Equals(SortDirection?.Trim(), "asc", StringComparison.OrdinalIgnoreCase);

    private static string? Sanitize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var cleaned = ControlCharRegex().Replace(value, string.Empty).Trim();
        if (cleaned.Length == 0)
            return null;

        return cleaned.Length > maxLength ? cleaned[..maxLength] : cleaned;
    }

    [GeneratedRegex(@"[\x00-\x1F\x7F]")]
    private static partial Regex ControlCharRegex();
}
