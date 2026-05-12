using Microsoft.AspNetCore.Mvc;

namespace B2BBuyback.Api.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(
            new
            {
                status = "Healthy",
                service = "B2B Buyback API",
                checkedAtUtc = DateTime.UtcNow
            });
    }
}
