using Microsoft.AspNetCore.Mvc;

namespace TournamentService.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", service = "tournament" });
    }
}
