using SecondHandShop.Application.Contracts.Analytics;

namespace SecondHandShop.Application.UseCases.Analytics;

/// <summary>
/// Read-only analytics queries backing the admin dashboard. Every method aggregates inside
/// the database — no per-row projection into memory — so the dashboard stays responsive as
/// the product and inquiry tables grow.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>
    /// Single round-trip that returns every section of the dashboard for a given range.
    /// </summary>
    Task<AnalyticsOverviewDto> GetOverviewAsync(
        AnalyticsDateRange range,
        CancellationToken cancellationToken = default);
}
