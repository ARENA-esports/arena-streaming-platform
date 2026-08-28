using Microsoft.AspNetCore.Mvc;

namespace UserService.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", service = "user",version = "1.0.0" });
    }
} // the version in the response it added to test the ci part. delete when merging.