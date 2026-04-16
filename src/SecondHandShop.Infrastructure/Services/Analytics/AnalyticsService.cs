using Microsoft.EntityFrameworkCore;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Contracts.Analytics;
using SecondHandShop.Application.UseCases.Analytics;
using SecondHandShop.Domain.Enums;
using SecondHandShop.Infrastructure.Persistence;

namespace SecondHandShop.Infrastructure.Services.Analytics;

/// <summary>
/// Database-backed analytics queries. All aggregations happen in SQL via EF Core GroupBy —
/// no product/sale/inquiry rows are materialized in memory during these calls, so the
/// dashboard remains cheap as the tables grow.
///
/// Category attribution uses <c>Product.CategoryId</c> (the primary category) rather than
/// the <c>ProductCategories</c> join table, so products that belong to multiple categories
/// are counted exactly once. This matches the "primary category for analytics" rule.
/// </summary>
public class AnalyticsService(
    IDbContextFactory<SecondHandShopDbContext> dbContextFactory,
    IClock clock,
    AnalyticsOptions analyticsOptions) : IAnalyticsService
{
    private const int HotUnsoldTopN = 10;
    private const int StaleStockTopN = 10;
    private const int SalesByCategoryTopN = 10;
    private const int DemandByCategoryTopN = 10;
    private sealed record SalesAggregate(int Count, decimal Revenue, decimal Average);

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(
        AnalyticsDateRange range,
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var start = ResolveRangeStart(range, now);

        var summaryTask = GetSummaryAsync(start, now, cancellationToken);
        var salesByCategoryTask = GetSalesByCategoryAsync(start, now, cancellationToken);
        var demandByCategoryTask = GetDemandByCategoryAsync(start, now, cancellationToken);
        var salesTrendTask = GetSalesTrendAsync(start, now, cancellationToken);
        var hotUnsoldTask = GetHotUnsoldProductsAsync(start, now, cancellationToken);
        var staleStockTask = GetStaleStockProductsAsync(start, now, cancellationToken);

        await Task.WhenAll(
            summaryTask,
            salesByCategoryTask,
            demandByCategoryTask,
            salesTrendTask,
            hotUnsoldTask,
            staleStockTask);

        return new AnalyticsOverviewDto(
            Range: range,
            RangeStartUtc: start,
            RangeEndUtc: now,
            Summary: await summaryTask,
            SalesByCategory: await salesByCategoryTask,
            DemandByCategory: await demandByCategoryTask,
            SalesTrend: await salesTrendTask,
            HotUnsoldProducts: await hotUnsoldTask,
            StaleStockProducts: await staleStockTask);
    }

    /// <summary>
    /// Null start means "all time" — callers that treat the start inclusively should fall back
    /// to an unbounded range when this is null.
    /// </summary>
    private static DateTime? ResolveRangeStart(AnalyticsDateRange range, DateTime nowUtc)
    {
        return range switch
        {
            AnalyticsDateRange.Last7Days => nowUtc.AddDays(-7),
            AnalyticsDateRange.Last30Days => nowUtc.AddDays(-30),
            AnalyticsDateRange.Last90Days => nowUtc.AddDays(-90),
            AnalyticsDateRange.Last12Months => nowUtc.AddMonths(-12),
            AnalyticsDateRange.AllTime => null,
            _ => nowUtc.AddDays(-30)
        };
    }

    private async Task<AnalyticsSummaryDto> GetSummaryAsync(
        DateTime? start,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var salesAggTask = GetSalesAggregateAsync(start, nowUtc, cancellationToken);
        var totalInquiriesTask = GetTotalInquiriesAsync(start, nowUtc, cancellationToken);
        var bestSellingTask = GetBestSellingCategoryAsync(start, nowUtc, cancellationToken);
        var mostInquiredTask = GetMostInquiredCategoryAsync(start, nowUtc, cancellationToken);

        await Task.WhenAll(salesAggTask, totalInquiriesTask, bestSellingTask, mostInquiredTask);

        var salesAgg = await salesAggTask;

        var totalSoldItems = salesAgg?.Count ?? 0;
        var totalRevenue = salesAgg?.Revenue ?? 0m;
        var averageSalePrice = salesAgg?.Average ?? 0m;
        var totalInquiries = await totalInquiriesTask;

        var conversionRate = totalInquiries == 0
            ? 0m
            : Math.Round((decimal)totalSoldItems / totalInquiries, 4);

        var attributionWindowDays = analyticsOptions.AttributionWindowDays;
        // The overview endpoint always reports a live range ending at "now". Under the cohort
        // maturity rule (rangeEnd + attributionWindow <= now), that means the current dashboard
        // window is never fully elapsed yet; recent inquiries can still convert later.
        var cohortWindowFullyElapsed = false;
        int? cohortInquiryCount = totalInquiries == 0 ? null : totalInquiries;
        int? cohortConversionsCount = totalInquiries == 0
            ? null
            : await GetCohortConversionsCountAsync(
                start,
                nowUtc,
                attributionWindowDays,
                cancellationToken);
        decimal? cohortConversionRate = totalInquiries == 0 || cohortConversionsCount is null
            ? null
            : Math.Round((decimal)cohortConversionsCount.Value / totalInquiries, 4);

        var bestSelling = await bestSellingTask;
        var mostInquired = await mostInquiredTask;

        return new AnalyticsSummaryDto(
            TotalSoldItems: totalSoldItems,
            TotalRevenue: totalRevenue,
            AverageSalePrice: Math.Round(averageSalePrice, 2),
            TotalInquiries: totalInquiries,
            InquiryToSaleConversionRate: conversionRate,
            CohortConversionRate: cohortConversionRate,
            CohortInquiryCount: cohortInquiryCount,
            CohortConversionCount: cohortConversionsCount,
            CohortAttributionWindowDays: attributionWindowDays,
            CohortWindowFullyElapsed: cohortWindowFullyElapsed,
            BestSellingCategoryName: bestSelling.Name,
            BestSellingCategoryId: bestSelling.Id,
            MostInquiredCategoryName: mostInquired.Name,
            MostInquiredCategoryId: mostInquired.Id);
    }

    private async Task<int> GetCohortConversionsCountAsync(
        DateTime? start,
        DateTime nowUtc,
        int attributionWindowDays,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var cohortQuery =
            from i in dbContext.Inquiries.AsNoTracking()
            join s in dbContext.ProductSales.AsNoTracking().Where(s => s.Status == SaleRecordStatus.Completed)
                on i.Id equals s.InquiryId
            where (!start.HasValue || (i.CreatedAt >= start.Value && i.CreatedAt <= nowUtc))
                && s.SoldAtUtc >= i.CreatedAt
                && s.SoldAtUtc <= i.CreatedAt.AddDays(attributionWindowDays)
            select i.Id;

        return await cohortQuery
            .Distinct()
            .CountAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<SalesByCategoryDto>> GetSalesByCategoryAsync(
        DateTime? start,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var salesQuery = dbContext.ProductSales
            .AsNoTracking()
            .Where(s => s.Status == SaleRecordStatus.Completed);
        if (start.HasValue)
        {
            var startValue = start.Value;
            salesQuery = salesQuery.Where(s => s.SoldAtUtc >= startValue && s.SoldAtUtc <= nowUtc);
        }

        var rows = await salesQuery
            .Join(
                dbContext.Products.AsNoTracking(),
                s => s.ProductId,
                p => p.Id,
                (s, p) => new { p.CategoryId, s.FinalSoldPrice })
            .GroupBy(x => x.CategoryId)
            .Select(g => new
            {
                CategoryId = g.Key,
                SoldCount = g.Count(),
                TotalRevenue = g.Sum(x => (decimal?)x.FinalSoldPrice) ?? 0m,
                AverageSalePrice = g.Average(x => (decimal?)x.FinalSoldPrice) ?? 0m
            })
            .OrderByDescending(x => x.TotalRevenue)
            .Take(SalesByCategoryTopN)
            .Join(
                dbContext.Categories.AsNoTracking(),
                x => x.CategoryId,
                c => c.Id,
                (x, c) => new
                {
                    CategoryId = c.Id,
                    CategoryName = c.Name,
                    x.SoldCount,
                    x.TotalRevenue,
                    x.AverageSalePrice
                })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new SalesByCategoryDto(
                r.CategoryId,
                r.CategoryName,
                r.SoldCount,
                Math.Round(r.TotalRevenue, 2),
                Math.Round(r.AverageSalePrice, 2)))
            .OrderByDescending(r => r.TotalRevenue)
            .ToList();
    }

    private async Task<IReadOnlyList<DemandByCategoryDto>> GetDemandByCategoryAsync(
        DateTime? start,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        // Inquiry counts per primary category in range.
        var inquiriesQuery = dbContext.Inquiries.AsNoTracking();
        if (start.HasValue)
        {
            var startValue = start.Value;
            inquiriesQuery = inquiriesQuery.Where(i => i.CreatedAt >= startValue && i.CreatedAt <= nowUtc);
        }

        var inquiryRows = await inquiriesQuery
            .Join(
                dbContext.Products.AsNoTracking(),
                i => i.ProductId,
                p => p.Id,
                (i, p) => new { p.CategoryId })
            .GroupBy(x => x.CategoryId)
            .Select(g => new { CategoryId = g.Key, InquiryCount = g.Count() })
            .ToListAsync(cancellationToken);

        // Sold counts per primary category in the same range.
        var salesQuery = dbContext.ProductSales
            .AsNoTracking()
            .Where(s => s.Status == SaleRecordStatus.Completed);
        if (start.HasValue)
        {
            var startValue = start.Value;
            salesQuery = salesQuery.Where(s => s.SoldAtUtc >= startValue && s.SoldAtUtc <= nowUtc);
        }

        var soldRows = await salesQuery
            .Join(
                dbContext.Products.AsNoTracking(),
                s => s.ProductId,
                p => p.Id,
                (s, p) => new { p.CategoryId })
            .GroupBy(x => x.CategoryId)
            .Select(g => new { CategoryId = g.Key, SoldCount = g.Count() })
            .ToListAsync(cancellationToken);

        var soldByCategory = soldRows.ToDictionary(x => x.CategoryId, x => x.SoldCount);

        // Collect all category ids we touched so we can look up names in one shot.
        var categoryIds = inquiryRows
            .Select(r => r.CategoryId)
            .Concat(soldRows.Select(r => r.CategoryId))
            .Distinct()
            .ToList();

        var categoryNames = await dbContext.Categories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);
        var categoryNameById = categoryNames.ToDictionary(x => x.Id, x => x.Name);

        var result = new List<DemandByCategoryDto>(inquiryRows.Count);
        foreach (var row in inquiryRows)
        {
            var soldCount = soldByCategory.GetValueOrDefault(row.CategoryId, 0);
            var inquiryCount = row.InquiryCount;
            var conversionRate = inquiryCount == 0
                ? 0m
                : Math.Round((decimal)soldCount / inquiryCount, 4);
            var heatScore = CalculateHeatScore(inquiryCount, soldCount, conversionRate);

            result.Add(new DemandByCategoryDto(
                CategoryId: row.CategoryId,
                CategoryName: categoryNameById.GetValueOrDefault(row.CategoryId) ?? "(unknown)",
                InquiryCount: inquiryCount,
                SoldCount: soldCount,
                ConversionRate: conversionRate,
                HeatScore: heatScore));
        }

        return result
            .OrderByDescending(r => r.HeatScore)
            .ThenByDescending(r => r.InquiryCount)
            .Take(DemandByCategoryTopN)
            .ToList();
    }

    /// <summary>
    /// Heat score blends raw demand (inquiries), realised demand (sold), and efficiency
    /// (conversion rate) into a single rankable number. Constants are deliberate:
    /// inquiries pull the score up linearly, sold items are worth twice an inquiry because
    /// a completed sale is a stronger demand signal than an open question, and the
    /// conversion-rate bonus (×10) only meaningfully fires once there are real sales.
    /// </summary>
    private static decimal CalculateHeatScore(int inquiryCount, int soldCount, decimal conversionRate)
    {
        var raw = inquiryCount * 1.0m + soldCount * 2.0m + conversionRate * 10.0m;
        return Math.Round(raw, 2);
    }

    private async Task<IReadOnlyList<SalesTrendPointDto>> GetSalesTrendAsync(
        DateTime? start,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var salesQuery = dbContext.ProductSales
            .AsNoTracking()
            .Where(s => s.Status == SaleRecordStatus.Completed);
        if (start.HasValue)
        {
            var startValue = start.Value;
            salesQuery = salesQuery.Where(s => s.SoldAtUtc >= startValue && s.SoldAtUtc <= nowUtc);
        }

        // Group by (Year, Month) instead of a constructed DateTime — the component accessors
        // are always translatable by Npgsql (EXTRACT(year ...), EXTRACT(month ...)) whereas a
        // `new DateTime(y, m, 1)` inside a LINQ tree can silently fall back to client eval and
        // pull every sale row into memory.
        var rows = await salesQuery
            .GroupBy(s => new { s.SoldAtUtc.Year, s.SoldAtUtc.Month })
            .Select(g => new
            {
                g.Key.Year,
                g.Key.Month,
                SoldCount = g.Count(),
                Revenue = g.Sum(s => (decimal?)s.FinalSoldPrice) ?? 0m
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new SalesTrendPointDto(
                new DateTime(r.Year, r.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                r.SoldCount,
                Math.Round(r.Revenue, 2)))
            .ToList();
    }

    private async Task<IReadOnlyList<HotUnsoldProductDto>> GetHotUnsoldProductsAsync(
        DateTime? start,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        // Inquiries in range, grouped by product.
        var inquiriesQuery = dbContext.Inquiries.AsNoTracking();
        if (start.HasValue)
        {
            var startValue = start.Value;
            inquiriesQuery = inquiriesQuery.Where(i => i.CreatedAt >= startValue && i.CreatedAt <= nowUtc);
        }

        var inquiryCounts = inquiriesQuery
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, InquiryCount = g.Count() });

        var rows = await (
            from p in dbContext.Products.AsNoTracking()
            where p.Status == ProductStatus.Available
            join ic in inquiryCounts on p.Id equals ic.ProductId
            join c in dbContext.Categories.AsNoTracking() on p.CategoryId equals c.Id
            orderby ic.InquiryCount descending, p.CreatedAt
            select new
            {
                ProductId = p.Id,
                p.Title,
                p.Slug,
                CategoryId = c.Id,
                CategoryName = c.Name,
                ic.InquiryCount,
                ListedPrice = p.Price,
                p.CreatedAt
            })
            .Take(HotUnsoldTopN)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new HotUnsoldProductDto(
                ProductId: r.ProductId,
                Title: r.Title,
                Slug: r.Slug,
                CategoryId: r.CategoryId,
                CategoryName: r.CategoryName,
                InquiryCount: r.InquiryCount,
                ListedPrice: r.ListedPrice,
                DaysListed: Math.Max(0, (int)(nowUtc - r.CreatedAt).TotalDays)))
            .ToList();
    }

    /// <summary>
    /// Stale stock: available products ranked by how long they've been listed, oldest first.
    /// Independent of the date range on the dashboard — a product listed three years ago is
    /// still stale even if we're viewing "last 7 days". The range only affects the
    /// <c>InquiryCount</c> column, which is included as context so admins can tell
    /// "old but still getting interest" from "old and ignored".
    /// </summary>
    private async Task<IReadOnlyList<HotUnsoldProductDto>> GetStaleStockProductsAsync(
        DateTime? start,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var inquiriesQuery = dbContext.Inquiries.AsNoTracking();
        if (start.HasValue)
        {
            var startValue = start.Value;
            inquiriesQuery = inquiriesQuery.Where(i => i.CreatedAt >= startValue && i.CreatedAt <= nowUtc);
        }

        // Pre-aggregated inquiry counts per product. Left-joined below so products with zero
        // in-range inquiries still show up (the whole point of a stale-stock list).
        var inquiryCounts = inquiriesQuery
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, InquiryCount = g.Count() });

        var rows = await (
            from p in dbContext.Products.AsNoTracking()
            where p.Status == ProductStatus.Available
            join c in dbContext.Categories.AsNoTracking() on p.CategoryId equals c.Id
            join ic in inquiryCounts on p.Id equals ic.ProductId into icj
            from ic in icj.DefaultIfEmpty()
            orderby p.CreatedAt // oldest listing first
            select new
            {
                ProductId = p.Id,
                p.Title,
                p.Slug,
                CategoryId = c.Id,
                CategoryName = c.Name,
                InquiryCount = (int?)(ic == null ? 0 : ic.InquiryCount) ?? 0,
                ListedPrice = p.Price,
                p.CreatedAt
            })
            .Take(StaleStockTopN)
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new HotUnsoldProductDto(
                ProductId: r.ProductId,
                Title: r.Title,
                Slug: r.Slug,
                CategoryId: r.CategoryId,
                CategoryName: r.CategoryName,
                InquiryCount: r.InquiryCount,
                ListedPrice: r.ListedPrice,
                DaysListed: Math.Max(0, (int)(nowUtc - r.CreatedAt).TotalDays)))
            .ToList();
    }

    private async Task<SalesAggregate?> GetSalesAggregateAsync(
        DateTime? start,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var salesQuery = dbContext.ProductSales
            .AsNoTracking()
            .Where(s => s.Status == SaleRecordStatus.Completed);
        if (start.HasValue)
        {
            var startValue = start.Value;
            salesQuery = salesQuery.Where(s => s.SoldAtUtc >= startValue && s.SoldAtUtc <= nowUtc);
        }

        return await salesQuery
            .GroupBy(_ => 1)
            .Select(g => new SalesAggregate(
                g.Count(),
                g.Sum(s => (decimal?)s.FinalSoldPrice) ?? 0m,
                g.Average(s => (decimal?)s.FinalSoldPrice) ?? 0m))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<int> GetTotalInquiriesAsync(
        DateTime? start,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var inquiriesQuery = dbContext.Inquiries.AsNoTracking();
        if (start.HasValue)
        {
            var startValue = start.Value;
            inquiriesQuery = inquiriesQuery.Where(i => i.CreatedAt >= startValue && i.CreatedAt <= nowUtc);
        }

        return await inquiriesQuery.CountAsync(cancellationToken);
    }

    private async Task<(Guid? Id, string? Name)> GetBestSellingCategoryAsync(
        DateTime? start,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var salesQuery = dbContext.ProductSales
            .AsNoTracking()
            .Where(s => s.Status == SaleRecordStatus.Completed);
        if (start.HasValue)
        {
            var startValue = start.Value;
            salesQuery = salesQuery.Where(s => s.SoldAtUtc >= startValue && s.SoldAtUtc <= nowUtc);
        }

        var result = await salesQuery
            .Join(
                dbContext.Products.AsNoTracking(),
                s => s.ProductId,
                p => p.Id,
                (s, p) => new { p.CategoryId })
            .GroupBy(x => x.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(1)
            .Join(
                dbContext.Categories.AsNoTracking(),
                x => x.CategoryId,
                c => c.Id,
                (x, c) => new { Id = (Guid?)c.Id, Name = c.Name })
            .FirstOrDefaultAsync(cancellationToken);

        return (result?.Id, result?.Name);
    }

    private async Task<(Guid? Id, string? Name)> GetMostInquiredCategoryAsync(
        DateTime? start,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var inquiriesQuery = dbContext.Inquiries.AsNoTracking();
        if (start.HasValue)
        {
            var startValue = start.Value;
            inquiriesQuery = inquiriesQuery.Where(i => i.CreatedAt >= startValue && i.CreatedAt <= nowUtc);
        }

        var result = await inquiriesQuery
            .Join(
                dbContext.Products.AsNoTracking(),
                i => i.ProductId,
                p => p.Id,
                (i, p) => new { p.CategoryId })
            .GroupBy(x => x.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(1)
            .Join(
                dbContext.Categories.AsNoTracking(),
                x => x.CategoryId,
                c => c.Id,
                (x, c) => new { Id = (Guid?)c.Id, Name = c.Name })
            .FirstOrDefaultAsync(cancellationToken);

        return (result?.Id, result?.Name);
    }
}
