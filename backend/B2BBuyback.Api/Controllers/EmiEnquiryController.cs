using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using B2BBuyback.Api.Data;
using B2BBuyback.Api.Models;
using B2BBuyback.Api.DTOs;

namespace B2BBuyback.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmiEnquiryController : ControllerBase
    {
        private readonly AppDbContext _context;
        public EmiEnquiryController(AppDbContext context) { _context = context; }

        // POST /api/EmiEnquiry
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] EmiEnquiryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.FullName))     return BadRequest("Full name required.");
            if (string.IsNullOrWhiteSpace(dto.MobileNumber)) return BadRequest("Mobile number required.");
            if (string.IsNullOrWhiteSpace(dto.PinCode))      return BadRequest("Pin code required.");

            var scootyExists = await _context.ScootyInventories.AnyAsync(s => s.ScootyId == dto.ScootyId);
            if (!scootyExists) return BadRequest("Vehicle not found.");

            _context.EmiEnquiries.Add(new EmiEnquiry
            {
                ScootyId     = dto.ScootyId,
                UserId       = dto.UserId?.Trim() ?? "guest",
                FullName     = dto.FullName.Trim(),
                MobileNumber = dto.MobileNumber.Trim(),
                PinCode      = dto.PinCode.Trim(),
                WantsLoan    = dto.WantsLoan,
                CreatedAt    = DateTime.UtcNow,
                Status       = "Pending"
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "EMI enquiry submitted! Our team will contact you soon." });
        }

        // GET /api/EmiEnquiry
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.EmiEnquiries
                .Include(e => e.Scooty).ThenInclude(s => s.Model)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new
                {
                    e.Id, e.UserId, e.FullName, e.MobileNumber, e.PinCode,
                    e.WantsLoan, e.Status, e.CreatedAt,
                    ModelName = e.Scooty.Model.ModelName
                })
                .ToListAsync();
            return Ok(data);
        }

        // PUT /api/EmiEnquiry/{id}/status
        // ── FIX: was [FromBody] string — Swagger can't generate schema for bare string body ──
        // Now uses a simple wrapper DTO
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest("Status is required.");

            var enquiry = await _context.EmiEnquiries.FindAsync(id);
            if (enquiry == null) return NotFound();

            enquiry.Status = dto.Status.Trim();
            await _context.SaveChangesAsync();
            return Ok(new { message = "Status updated.", status = enquiry.Status });
        }
    }
}
