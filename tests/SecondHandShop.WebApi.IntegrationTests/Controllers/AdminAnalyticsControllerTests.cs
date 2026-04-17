using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using SecondHandShop.Application.Contracts.Analytics;
using SecondHandShop.WebApi.Contracts;
using SecondHandShop.WebApi.IntegrationTests.Infrastructure;

namespace SecondHandShop.WebApi.IntegrationTests.Controllers;

[Collection("WebApiIntegration")]
public class AdminAnalyticsControllerTests
{
    private readonly TestWebApplicationFactory _factory;

    public AdminAnalyticsControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAppMocks();
    }

    [Fact]
    public async Task GetOverview_ShouldReturn200_ForExplicitRange()
    {
        var overview = CreateOverview(AnalyticsDateRange.Last7Days);
        _factory.AnalyticsServiceMock
            .Setup(x => x.GetOverviewAsync(AnalyticsDateRange.Last7Days, It.IsAny<CancellationToken>()))
            .ReturnsAsync(overview);

        using var client = CreateAdminClient();

        var response = await client.GetAsync("/api/lord/analytics/overview?range=7d");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AnalyticsOverviewDto>();
        body.Should().NotBeNull();
        body!.Range.Should().Be(AnalyticsDateRange.Last7Days);
    }

    [Fact]
    public async Task GetOverview_ShouldUseDefaultRange_WhenRangeIsMissing()
    {
        var overview = CreateOverview(AnalyticsDateRange.Last30Days);
        _factory.AnalyticsServiceMock
            .Setup(x => x.GetOverviewAsync(AnalyticsDateRange.Last30Days, It.IsAny<CancellationToken>()))
            .ReturnsAsync(overview);

        using var client = CreateAdminClient();

        var response = await client.GetAsync("/api/lord/analytics/overview");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.AnalyticsServiceMock.Verify(x => x.GetOverviewAsync(AnalyticsDateRange.Last30Days, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOverview_ShouldReturn400_WhenRangeIsUnsupported()
    {
        using var client = CreateAdminClient();

        var response = await client.GetAsync("/api/lord/analytics/overview?range=13d");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        body.Should().NotBeNull();
        body!.Message.Should().Be("Unsupported range '13d'. Use one of: 7d, 30d, 90d, 12m, all.");
    }

    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        client.DefaultRequestHeaders.Add(
            "Cookie",
            TestWebApplicationFactory.CreateCookieHeader(_factory.CreateAdminToken()));

        return client;
    }

    private static AnalyticsOverviewDto CreateOverview(AnalyticsDateRange range)
    {
        return new AnalyticsOverviewDto(
            range,
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 17, 0, 0, 0, DateTimeKind.Utc),
            new AnalyticsSummaryDto(1, 220m, 220m, 2, 0.5m, 0.5m, 2, 1, 30, false, "Bags", Guid.NewGuid(), "Bags", Guid.NewGuid()),
            [new SalesByCategoryDto(Guid.NewGuid(), "Bags", 1, 220m, 220m)],
            [new DemandByCategoryDto(Guid.NewGuid(), "Bags", 2, 1, 0.5m, 8m)],
            [new SalesTrendPointDto(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), 1, 220m)],
            [new HotUnsoldProductDto(Guid.NewGuid(), "Vintage bag", "vintage-bag", Guid.NewGuid(), "Bags", 3, 250m, 10)],
            [new HotUnsoldProductDto(Guid.NewGuid(), "Old coat", "old-coat", Guid.NewGuid(), "Coats", 0, 120m, 90)]);
    }
}
