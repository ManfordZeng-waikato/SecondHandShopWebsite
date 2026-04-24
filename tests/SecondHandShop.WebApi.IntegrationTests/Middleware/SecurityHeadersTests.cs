using FluentAssertions;
using SecondHandShop.WebApi.IntegrationTests.Infrastructure;

namespace SecondHandShop.WebApi.IntegrationTests.Middleware;

[Collection("WebApiIntegration")]
public class SecurityHeadersTests
{
    private readonly TestWebApplicationFactory _factory;

    public SecurityHeadersTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAppMocks();
    }

    [Theory]
    [InlineData("/api/categories")]
    [InlineData("/api/products/featured")]
    [InlineData("/api/lord/products")] // auth-rejected, but headers must still apply
    public async Task Response_ShouldCarryBaselineSecurityHeaders(string path)
    {
        using var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync(path);

        HeaderValue(response, "X-Content-Type-Options").Should().Be("nosniff");
        HeaderValue(response, "X-Frame-Options").Should().Be("DENY");
        HeaderValue(response, "Referrer-Policy").Should().Be("no-referrer");
        HeaderValue(response, "Permissions-Policy").Should().Be("camera=(), microphone=(), geolocation=()");
    }

    private static string? HeaderValue(HttpResponseMessage response, string name)
    {
        if (response.Headers.TryGetValues(name, out var values))
            return values.FirstOrDefault();
        if (response.Content.Headers.TryGetValues(name, out var contentValues))
            return contentValues.FirstOrDefault();
        return null;
    }
}
