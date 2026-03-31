using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SecondHandShop.Application.Contracts.Admin;
using SecondHandShop.Application.UseCases.Admin.ChangeInitialPassword;
using SecondHandShop.Application.UseCases.Admin.Login;
using SecondHandShop.Application.UseCases.Admin.Me;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/lord/auth")]
public class AdminAuthController(IMediator mediator) : ControllerBase
{
    internal const string CookieName = "shs.admin.token";

    [HttpPost("login")]
    [EnableRateLimiting("LoginRateLimit")]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginAdminRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginAdminCommand(request.UserName, request.Password);
        var response = await mediator.Send(command, cancellationToken);

        Response.Cookies.Append(CookieName, response.Token, BuildCookieOptions(response.ExpiresAt));

        return Ok(new
        {
            expiresAt = response.ExpiresAt,
            requiresPasswordChange = response.RequiresPasswordChange
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(CookieName, BuildCookieOptions(DateTimeOffset.UtcNow));
        return NoContent();
    }

    [HttpPost("change-initial-password")]
    [Authorize(Policy = "AdminSession")]
    public async Task<IActionResult> ChangeInitialPasswordAsync(
        [FromBody] ChangeAdminInitialPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = ResolveAdminUserId();
        if (adminId is null)
            return Unauthorized();

        var command = new ChangeAdminInitialPasswordCommand(
            adminId.Value,
            request.CurrentPassword,
            request.NewPassword,
            request.ConfirmNewPassword);

        await mediator.Send(command, cancellationToken);

        // End server session: user must authenticate again with the new password (avoids carrying old JWT).
        Response.Cookies.Delete(CookieName, BuildCookieOptions(DateTimeOffset.UtcNow));

        return Ok(new
        {
            success = true,
            requiresReLogin = true,
            message = "Password changed successfully, please log in again."
        });
    }

    /// <summary>
    /// Returns the current admin from the database (JWT only identifies the subject). Use this to sync SPA state after refresh.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = "AdminSession")]
    public async Task<IActionResult> MeAsync(CancellationToken cancellationToken)
    {
        var adminId = ResolveAdminUserId();
        if (adminId is null)
            return Unauthorized();

        var me = await mediator.Send(new GetAdminMeQuery(adminId.Value), cancellationToken);
        if (me is null)
            return Unauthorized();

        return Ok(me);
    }

    private Guid? ResolveAdminUserId()
    {
        var raw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    private static CookieOptions BuildCookieOptions(DateTimeOffset expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/lord",
        Expires = expires
    };
}
