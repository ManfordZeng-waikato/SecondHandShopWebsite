using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/admin/ping")]
[Authorize(Policy = "AdminOnly")]
public class AdminPingController : ControllerBase
{
    [HttpGet]
    public IActionResult Ping()
    {
        return Ok(new { Message = "pong", Timestamp = DateTimeOffset.UtcNow });
    }
}
