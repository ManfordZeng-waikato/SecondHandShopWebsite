using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/lord/ping")]
[Authorize(Policy = "AdminFullAccess")]
public class AdminPingController : ControllerBase
{
    [HttpGet]
    public IActionResult Ping()
    {
        return Ok(new { Message = "pong", Timestamp = DateTimeOffset.UtcNow });
    }
}
