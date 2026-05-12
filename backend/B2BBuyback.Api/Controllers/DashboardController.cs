using B2BBuyback.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace B2BBuyback.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IBuybackService _buybackService;

    public DashboardController(IBuybackService buybackService)
    {
        _buybackService = buybackService;
    }

    [HttpGet("summary")]
    public IActionResult GetSummary()
    {
        return Ok(_buybackService.GetSummary());
    }
}
