namespace SecondHandShop.Application.Contracts.Analytics;

/// <summary>
/// Everything the admin analytics dashboard needs for a single date range, returned in one
/// payload so the UI only makes one round-trip per range change.
/// </summary>
public sealed record AnalyticsOverviewDto(
    AnalyticsDateRange Range,
    DateTime? RangeStartUtc,
    DateTime RangeEndUtc,
    AnalyticsSummaryDto Summary,
    IReadOnlyList<SalesByCategoryDto> SalesByCategory,
    IReadOnlyList<DemandByCategoryDto> DemandByCategory,
    IReadOnlyList<SalesTrendPointDto> SalesTrend,
    IReadOnlyList<HotUnsoldProductDto> HotUnsoldProducts,
    IReadOnlyList<HotUnsoldProductDto> StaleStockProducts);
