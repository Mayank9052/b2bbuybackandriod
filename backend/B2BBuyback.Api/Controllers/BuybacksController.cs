using B2BBuyback.Api.Models;
using B2BBuyback.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace B2BBuyback.Api.Controllers;

[ApiController]
[Route("api/buybacks")]
public class BuybacksController : ControllerBase
{
    private readonly IBuybackService _buybackService;

    public BuybacksController(IBuybackService buybackService)
    {
        _buybackService = buybackService;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(_buybackService.GetRequests());
    }

    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var request = _buybackService.GetRequestById(id);
        if (request is null)
        {
            return NotFound();
        }

        return Ok(request);
    }

    [HttpPost]
    public IActionResult Create(CreateBuybackRequest request)
    {
        var createdRequest = _buybackService.CreateRequest(request);

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdRequest.Id },
            createdRequest);
    }
}
