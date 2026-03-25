namespace SecondHandShop.Application.Contracts.Customers;

public sealed record CustomerInquiryQueryParameters
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public int SafePage => Math.Max(1, Page);
    public int SafePageSize => Math.Clamp(PageSize, 1, 100);
}
