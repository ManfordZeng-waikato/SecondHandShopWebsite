namespace SecondHandShop.Application.Contracts.Analytics;

public sealed record SalesByCategoryDto(
    Guid CategoryId,
    string CategoryName,
    int SoldCount,
    decimal TotalRevenue,
    decimal AverageSalePrice);
