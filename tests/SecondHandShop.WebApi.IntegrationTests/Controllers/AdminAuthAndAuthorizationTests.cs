using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MediatR;
using Moq;
using SecondHandShop.Application.Contracts.Admin;
using SecondHandShop.Application.UseCases.Admin.ChangeInitialPassword;
using SecondHandShop.WebApi.IntegrationTests.Infrastructure;

namespace SecondHandShop.WebApi.IntegrationTests.Controllers;

[Collection("WebApiIntegration")]
public class AdminAuthAndAuthorizationTests
{
    private readonly TestWebApplicationFactory _factory;

    public AdminAuthAndAuthorizationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.ResetAppMocks();
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
    public async Task ChangeInitialPassword_ShouldBeAllowed_WhenTokenRequiresPasswordChange()
    {
        _factory.MediatorMock
            .Setup(x => x.Send(It.IsAny<ChangeAdminInitialPasswordCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        using var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add("Cookie", TestWebApplicationFactory.CreateCookieHeader(
            _factory.CreateAdminToken(requiresPasswordChange: true)));

        var response = await client.PostAsJsonAsync(
            "/api/lord/auth/change-initial-password",
            new ChangeAdminInitialPasswordRequest
            {
                CurrentPassword = "old-password",
                NewPassword = "New-Password-123!",
                ConfirmNewPassword = "New-Password-123!"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "AdminSession policy must accept pwd_chg_req tokens so the user can complete the forced password change");
    }

    [Fact]
    public async Task AdminCustomers_ShouldReturn403_WhenTokenRequiresPasswordChange()
    {
        using var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add("Cookie", TestWebApplicationFactory.CreateCookieHeader(
            _factory.CreateAdminToken(requiresPasswordChange: true)));

        var response = await client.GetAsync("/api/lord/customers");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "AdminFullAccess must reject tokens carrying pwd_chg_req until password is changed");
    }

    [Fact]
    public async Task AdminProducts_ShouldReturn401_WhenTokenVersionDoesNotMatchCurrentUser()
    {
        // Active admin in the factory starts at TokenVersion=0;
        // a token minted with tokenVersion=99 simulates a revoked session.
        using var client = _factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
        client.DefaultRequestHeaders.Add("Cookie", TestWebApplicationFactory.CreateCookieHeader(
            _factory.CreateAdminToken(tokenVersion: 99)));

        var response = await client.GetAsync("/api/lord/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "OnTokenValidated must reject tokens whose version no longer matches AdminUser.TokenVersion");
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
