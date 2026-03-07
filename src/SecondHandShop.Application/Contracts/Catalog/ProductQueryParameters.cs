namespace SecondHandShop.Application.Contracts.Catalog;

public sealed record ProductQueryParameters
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 24;
    public string? Category { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public string? Status { get; init; }
    public string? Sort { get; init; }

    public int SafePage => Math.Max(1, Page);
    public int SafePageSize => Math.Clamp(PageSize, 1, 100);
}
