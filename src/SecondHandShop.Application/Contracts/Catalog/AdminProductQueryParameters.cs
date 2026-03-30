namespace SecondHandShop.Application.Contracts.Catalog;

public sealed record AdminProductQueryParameters
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string? Status { get; init; }
    public Guid? CategoryId { get; init; }
    public bool? IsFeatured { get; init; }

    public int SafePage => Math.Max(1, Page);
    public int SafePageSize => Math.Clamp(PageSize, 1, 100);
}
