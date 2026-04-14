namespace SecondHandShop.Application.Contracts.Analytics;

public sealed record AnalyticsSummaryDto(
    int TotalSoldItems,
    decimal TotalRevenue,
    decimal AverageSalePrice,
    int TotalInquiries,
    decimal InquiryToSaleConversionRate,
    decimal? CohortConversionRate,
    int? CohortInquiryCount,
    int? CohortConversionCount,
    int CohortAttributionWindowDays,
    bool CohortWindowFullyElapsed,
    string? BestSellingCategoryName,
    Guid? BestSellingCategoryId,
    string? MostInquiredCategoryName,
    Guid? MostInquiredCategoryId);
