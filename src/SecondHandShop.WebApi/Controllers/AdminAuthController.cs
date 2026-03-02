using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SecondHandShop.Application.Contracts.Admin;
using SecondHandShop.Application.UseCases.Admin.Login;

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
        try
        {
            var command = new LoginAdminCommand(request.UserName, request.Password);
            var response = await mediator.Send(command, cancellationToken);

            Response.Cookies.Append(CookieName, response.Token, BuildCookieOptions(response.ExpiresAt));

            return Ok(new { expiresAt = response.ExpiresAt });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse("Invalid credentials"));
        }
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(CookieName, BuildCookieOptions(DateTimeOffset.UtcNow));
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = User.FindFirstValue(ClaimTypes.Name);
        return Ok(new { userId, userName });
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
