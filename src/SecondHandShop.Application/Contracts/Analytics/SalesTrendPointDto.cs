namespace SecondHandShop.Application.Contracts.Analytics;

/// <summary>
/// One data point on the monthly sales trend. <see cref="MonthStartUtc"/> is the first day of
/// the month in UTC (e.g. 2026-04-01T00:00:00Z).
/// </summary>
public sealed record SalesTrendPointDto(
    DateTime MonthStartUtc,
    int SoldCount,
    decimal Revenue);
