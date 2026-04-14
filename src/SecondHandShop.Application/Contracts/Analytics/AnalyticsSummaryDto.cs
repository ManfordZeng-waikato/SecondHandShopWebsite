namespace SecondHandShop.Application.Contracts.Analytics;

public sealed record AnalyticsSummaryDto(
    int TotalSoldItems,
    decimal TotalRevenue,
    decimal AverageSalePrice,
    int TotalInquiries,
    decimal InquiryToSaleConversionRate,
    string? BestSellingCategoryName,
    Guid? BestSellingCategoryId,
    string? MostInquiredCategoryName,
    Guid? MostInquiredCategoryId);
