using System.Net;
using FluentAssertions;
using SecondHandShop.WebApi.IntegrationTests.Infrastructure;

namespace SecondHandShop.WebApi.IntegrationTests.Controllers;

[Collection("WebApiIntegration")]
public class AdminAuthAndAuthorizationTests
{
    private readonly TestWebApplicationFactory _factory;

    public AdminAuthAndAuthorizationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminProducts_ShouldReturn401_WhenRequestHasNoAdminSession()
    {
        using var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.GetAsync("/api/lord/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminProducts_ShouldReturn403_WhenTokenRequiresPasswordChange()
    {
        using var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add("Cookie", TestWebApplicationFactory.CreateCookieHeader(
            _factory.CreateAdminToken(requiresPasswordChange: true)));

        var response = await client.GetAsync("/api/lord/products");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Logout_ShouldClearAdminCookie()
    {
        using var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        var response = await client.PostAsync("/api/lord/auth/logout", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders).Should().BeTrue();
        cookieHeaders!.Single().Should().Contain("shs.admin.token=");
        cookieHeaders!.Single().Should().Contain("expires=");
    }
}
