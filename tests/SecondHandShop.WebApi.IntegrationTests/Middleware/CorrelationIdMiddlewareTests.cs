using FluentAssertions;
using SecondHandShop.WebApi.IntegrationTests.Infrastructure;

namespace SecondHandShop.WebApi.IntegrationTests.Middleware;

[Collection("WebApiIntegration")]
public class CorrelationIdMiddlewareTests
{
    private const string HeaderName = "X-Correlation-Id";
    private readonly TestWebApplicationFactory _factory;

    public CorrelationIdMiddlewareTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAppMocks();
    }

    [Fact]
    public async Task Response_ShouldEchoIncomingCorrelationId()
    {
        var incomingId = $"test-corr-{Guid.NewGuid():N}";
        using var client = CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/categories");
        request.Headers.Add(HeaderName, incomingId);

        using var response = await client.SendAsync(request);

        response.Headers.TryGetValues(HeaderName, out var values).Should().BeTrue();
        values!.Single().Should().Be(incomingId);
    }

    [Fact]
    public async Task Response_ShouldGenerateCorrelationId_WhenHeaderIsMissing()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/api/categories");

        response.Headers.TryGetValues(HeaderName, out var values).Should().BeTrue();
        values!.Single().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Response_ShouldGenerateCorrelationId_WhenHeaderIsBlank()
    {
        using var client = CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/categories");
        request.Headers.TryAddWithoutValidation(HeaderName, "   ");

        using var response = await client.SendAsync(request);

        response.Headers.TryGetValues(HeaderName, out var values).Should().BeTrue();
        var value = values!.Single();
        value.Should().NotBeNullOrWhiteSpace();
        value.Trim().Should().NotBeEmpty();
    }

    private HttpClient CreateClient() => _factory.CreateClient(new()
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    });
}
