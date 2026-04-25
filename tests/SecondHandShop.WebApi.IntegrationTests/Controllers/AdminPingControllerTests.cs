using System.Net;
using FluentAssertions;
using SecondHandShop.WebApi.IntegrationTests.Infrastructure;

namespace SecondHandShop.WebApi.IntegrationTests.Controllers;

[Collection("WebApiIntegration")]
public class AdminPingControllerTests
{
    private readonly TestWebApplicationFactory _factory;

    public AdminPingControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAppMocks();
    }

    [Fact]
    public async Task Ping_ShouldReturn401_WhenNoAdminCookieIsPresent()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/lord/ping");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Ping_ShouldReturn200_WithValidFullAccessCookie()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", TestWebApplicationFactory.CreateCookieHeader(
            _factory.CreateAdminToken()));

        var response = await client.GetAsync("/api/lord/ping");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("pong");
    }

    [Fact]
    public async Task Ping_ShouldReturn403_WhenTokenRequiresPasswordChange()
    {
        using var client = CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", TestWebApplicationFactory.CreateCookieHeader(
            _factory.CreateAdminToken(requiresPasswordChange: true)));

        var response = await client.GetAsync("/api/lord/ping");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "ping is protected by AdminFullAccess, which rejects pwd_chg_req tokens");
    }

    private HttpClient CreateClient() => _factory.CreateClient(new()
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    });
}
