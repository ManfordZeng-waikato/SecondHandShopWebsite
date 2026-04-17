using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using SecondHandShop.Application.Contracts.Admin;
using SecondHandShop.Application.UseCases.Admin.ChangeInitialPassword;
using SecondHandShop.Application.UseCases.Admin.Login;
using SecondHandShop.Application.UseCases.Admin.Me;
using SecondHandShop.Application.UseCases.Admin.RefreshSession;
using SecondHandShop.WebApi.Authentication;
using SecondHandShop.WebApi.IntegrationTests.Infrastructure;

namespace SecondHandShop.WebApi.IntegrationTests.Controllers;

[Collection("WebApiIntegration")]
public class AdminAuthControllerTests
{
    private readonly TestWebApplicationFactory _factory;

    public AdminAuthControllerTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAppMocks();
    }

    [Fact]
    public async Task Login_ShouldReturn200_AndWriteCookieAndSessionHeader()
    {
        var expiresAt = new DateTimeOffset(new DateTime(2026, 4, 17, 1, 0, 0, DateTimeKind.Utc));
        _factory.MediatorMock
            .Setup(x => x.Send(
                It.Is<LoginAdminCommand>(command =>
                    command.UserName == "lord"
                    && command.Password == "correct-password"
                    && command.SourceIpAddress == "203.0.113.7"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginAdminResponse("jwt-token", expiresAt, true));

        using var client = CreateClient(("CF-Connecting-IP", "203.0.113.7"));

        var response = await client.PostAsJsonAsync("/api/lord/auth/login", new LoginAdminRequest("lord", "correct-password"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders).Should().BeTrue();
        cookieHeaders!.Single().Should().Contain("shs.admin.token=jwt-token");
        cookieHeaders!.Single().Should().Contain("httponly");
        response.Headers.TryGetValues(AdminAuthCookies.SessionExpiresHeaderName, out var sessionHeaders).Should().BeTrue();
        sessionHeaders!.Single().Should().Be(expiresAt.ToString("o"));
    }

    [Fact]
    public async Task Refresh_ShouldReturn401_WhenUnauthenticated()
    {
        using var client = CreateClient();

        var response = await client.PostAsync("/api/lord/auth/refresh", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ShouldReturn200_AndRefreshCookieAndSessionHeader()
    {
        var expiresAt = new DateTimeOffset(new DateTime(2026, 4, 17, 2, 0, 0, DateTimeKind.Utc));
        _factory.MediatorMock
            .Setup(x => x.Send(
                It.Is<RefreshAdminSessionCommand>(command => command.AdminUserId == _factory.ActiveAdminId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoginAdminResponse("fresh-token", expiresAt, false));

        using var client = CreateAdminClient();

        var response = await client.PostAsync("/api/lord/auth/refresh", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders).Should().BeTrue();
        cookieHeaders!.Single().Should().Contain("shs.admin.token=fresh-token");
        response.Headers.TryGetValues(AdminAuthCookies.SessionExpiresHeaderName, out var sessionHeaders).Should().BeTrue();
        sessionHeaders!.Single().Should().Be(expiresAt.ToString("o"));
    }

    [Fact]
    public async Task Me_ShouldReturn200_WhenMediatorReturnsAdmin()
    {
        var me = new AdminMeResponse(
            true,
            _factory.ActiveAdminId,
            "lord",
            "lord@admin.local",
            "Admin",
            true);

        _factory.MediatorMock
            .Setup(x => x.Send(
                It.Is<GetAdminMeQuery>(query => query.AdminUserId == _factory.ActiveAdminId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(me);

        using var client = CreateAdminClient();

        var response = await client.GetAsync("/api/lord/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AdminMeResponse>();
        body.Should().NotBeNull();
        body!.UserId.Should().Be(_factory.ActiveAdminId);
        body.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeInitialPassword_ShouldReturn200_AndClearCookie()
    {
        _factory.MediatorMock
            .Setup(x => x.Send(
                It.Is<ChangeAdminInitialPasswordCommand>(command =>
                    command.AdminUserId == _factory.ActiveAdminId
                    && command.CurrentPassword == "old-password"
                    && command.NewPassword == "new-password"
                    && command.ConfirmNewPassword == "new-password"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        using var client = CreateAdminClient();

        var response = await client.PostAsJsonAsync("/api/lord/auth/change-initial-password", new ChangeAdminInitialPasswordRequest
        {
            CurrentPassword = "old-password",
            NewPassword = "new-password",
            ConfirmNewPassword = "new-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders).Should().BeTrue();
        cookieHeaders!.Single().Should().Contain("shs.admin.token=");
        cookieHeaders!.Single().Should().Contain("expires=");
    }

    private HttpClient CreateAdminClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(
            "Cookie",
            TestWebApplicationFactory.CreateCookieHeader(_factory.CreateAdminToken()));
        return client;
    }

    private HttpClient CreateClient(params (string Name, string Value)[] headers)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        foreach (var (name, value) in headers)
        {
            client.DefaultRequestHeaders.Add(name, value);
        }

        return client;
    }
}
