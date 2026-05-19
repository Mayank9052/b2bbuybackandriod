using B2BBuyback.Api.Data;
using B2BBuyback.Api.DTOs;
using B2BBuyback.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace B2BBuyback.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerRequestsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CustomerRequestsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomerRequests()
        {
            var requests = await _context.CustomerRequests
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return Ok(requests);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomerRequest([FromBody] CreateCustomerRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var request = new CustomerRequest
                {
                    CustomerName = dto.CustomerName?.Trim() ?? string.Empty,
                    MobileNumber = dto.MobileNumber?.Trim() ?? string.Empty,
                    Email = dto.Email?.Trim(),
                    City = dto.City?.Trim() ?? string.Empty,
                    State = dto.State?.Trim(),
                    Gender = dto.Gender?.Trim(),
                    PreferredModel = dto.PreferredModel?.Trim(),
                    RequestType = dto.RequestType?.Trim(),
                    PreferredContact = dto.PreferredContact?.Trim(),
                    Notes = dto.Notes?.Trim(),
                };

                _context.CustomerRequests.Add(request);
                await _context.SaveChangesAsync();

                return Ok(request);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomerRequest(int id, [FromBody] CreateCustomerRequestDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var request = await _context.CustomerRequests.FindAsync(id);
            if (request == null)
                return NotFound();

            try
            {
                request.CustomerName = dto.CustomerName?.Trim() ?? string.Empty;
                request.MobileNumber = dto.MobileNumber?.Trim() ?? string.Empty;
                request.Email = dto.Email?.Trim();
                request.City = dto.City?.Trim() ?? string.Empty;
                request.State = dto.State?.Trim();
                request.Gender = dto.Gender?.Trim();
                request.PreferredModel = dto.PreferredModel?.Trim();
                request.RequestType = dto.RequestType?.Trim();
                request.PreferredContact = dto.PreferredContact?.Trim();
                request.Notes = dto.Notes?.Trim();

                await _context.SaveChangesAsync();
                return Ok(request);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomerRequest(int id)
        {
            var request = await _context.CustomerRequests.FindAsync(id);
            if (request == null)
                return NotFound();

            _context.CustomerRequests.Remove(request);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
