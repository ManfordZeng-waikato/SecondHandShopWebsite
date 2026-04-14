using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecondHandShop.Application.Contracts.Analytics;
using SecondHandShop.Application.UseCases.Analytics;
using SecondHandShop.WebApi.Contracts;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/lord/analytics")]
[Authorize(Policy = "AdminFullAccess")]
public class AdminAnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    /// <summary>
    /// Single-shot analytics payload for the admin dashboard. <paramref name="range"/> accepts
    /// <c>7d</c>, <c>30d</c>, <c>90d</c>, <c>12m</c>, or <c>all</c>; defaults to 30 days.
    /// </summary>
    [HttpGet("overview")]
    public async Task<ActionResult<AnalyticsOverviewDto>> GetOverviewAsync(
        [FromQuery] string? range,
        CancellationToken cancellationToken)
    {
        if (!TryParseRange(range, out var parsedRange))
        {
            return BadRequest(new ErrorResponse(
                $"Unsupported range '{range}'. Use one of: 7d, 30d, 90d, 12m, all."));
        }

        var result = await analyticsService.GetOverviewAsync(parsedRange, cancellationToken);
        return Ok(result);
    }

    private static bool TryParseRange(string? raw, out AnalyticsDateRange range)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            range = AnalyticsDateRange.Last30Days;
            return true;
        }

        switch (raw.Trim().ToLowerInvariant())
        {
            case "7d":
            case "7":
                range = AnalyticsDateRange.Last7Days;
                return true;
            case "30d":
            case "30":
                range = AnalyticsDateRange.Last30Days;
                return true;
            case "90d":
            case "90":
                range = AnalyticsDateRange.Last90Days;
                return true;
            case "12m":
            case "1y":
            case "365d":
                range = AnalyticsDateRange.Last12Months;
                return true;
            case "all":
            case "alltime":
            case "all-time":
                range = AnalyticsDateRange.AllTime;
                return true;
            default:
                range = default;
                return false;
        }
    }
}
