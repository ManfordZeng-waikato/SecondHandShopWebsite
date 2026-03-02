using MediatR;
using Microsoft.AspNetCore.Mvc;
using SecondHandShop.Application.Contracts.Admin;
using SecondHandShop.Application.UseCases.Admin.Login;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController(IMediator mediator) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginAdminRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new LoginAdminCommand(request.UserName, request.Password);
            var response = await mediator.Send(command, cancellationToken);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new ErrorResponse("Invalid credentials"));
        }
    }
}
