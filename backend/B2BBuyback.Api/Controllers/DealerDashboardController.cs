using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using B2BBuyback.Api.Data;
using B2BBuyback.Api.DTOs;

namespace B2BBuyback.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Dealer")]   // ← re-enabled (was commented out)
    public class DealerDashboardController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DealerDashboardController(AppDbContext context)
        {
            _context = context;
        }

        // ── GET api/DealerDashboard/stats ─────────────────────────
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            // DealerCode claim is set in DealerAuthController.GenerateJwt()
            var dealerCode = User.FindFirstValue("DealerCode");

            // If no DealerCode claim, try Name claim as fallback
            if (string.IsNullOrEmpty(dealerCode))
                dealerCode = User.FindFirstValue(ClaimTypes.Name);

            if (string.IsNullOrEmpty(dealerCode))
                return Unauthorized(new { error = "Dealer identity not found in token." });

            var allCases = await _context.ExchangeCases
                .Where(c => c.DealerId == dealerCode)
                .ToListAsync();

            var now        = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var dashboard = new DealerDashboardDto
            {
                ProcuredThisMonth = allCases.Count(c => c.CreatedAt >= monthStart),
                PendingApproval   = allCases.Count(c => c.Status == "PendingAdminReview"),
                SoldAndSettled    = allCases.Count(c => c.Status == "Sold" || c.Status == "Settled"),
                InRefurbishment   = allCases.Count(c => c.Status == "Refurbishment"),
                Listed            = allCases.Count(c => c.Status == "Listed"),

                RecentVehicles = allCases
                    .OrderByDescending(c => c.CreatedAt)
                    .Take(10)
                    .Select(c => new VehicleSummaryDto
                    {
                        CaseId         = c.Id,
                        CaseNumber     = c.CaseNumber,
                        CustomerName   = c.CustomerName,
                        VehicleModel   = c.VehicleModel,
                        VehicleVariant = c.VehicleVariant ?? "",
                        RegistrationNo = c.RegistrationNo,
                        Status         = c.Status,
                        SubmittedAt    = c.SubmittedAt,
                        ApprovedPrice  = c.ApprovedPrice,
                    })
                    .ToList(),
            };

            return Ok(dashboard);
        }

        // ── GET api/DealerDashboard/profile ──────────────────────
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var dealerIdStr = User.FindFirstValue("DealerId");
            if (!int.TryParse(dealerIdStr, out var dealerId))
                return Unauthorized(new { error = "DealerId not found in token." });

            var dealer = await _context.Dealers.FindAsync(dealerId);
            if (dealer == null) return NotFound();

            return Ok(new DealerProfileDto
            {
                DealerId   = dealer.DealerId,
                DealerCode = dealer.DealerCode,
                DealerName = dealer.DealerName,
                Mobile     = dealer.MobileNumber,
                City       = dealer.City,
                State      = dealer.State,
            });
        }

        // ── GET api/DealerDashboard/vehicles ─────────────────────
        [HttpGet("vehicles")]
        public async Task<IActionResult> GetVehicles(
            [FromQuery] string? status,
            [FromQuery] int page = 1)
        {
            var dealerCode = User.FindFirstValue("DealerCode");
            if (string.IsNullOrEmpty(dealerCode))
                return Unauthorized(new { error = "DealerCode not found in token." });

            var query = _context.ExchangeCases
                .Where(c => c.DealerId == dealerCode);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(c => c.Status == status);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * 20)
                .Take(20)
                .Select(c => new VehicleSummaryDto
                {
                    CaseId         = c.Id,
                    CaseNumber     = c.CaseNumber,
                    CustomerName   = c.CustomerName,
                    VehicleModel   = c.VehicleModel,
                    VehicleVariant = c.VehicleVariant ?? "",
                    RegistrationNo = c.RegistrationNo,
                    Status         = c.Status,
                    SubmittedAt    = c.SubmittedAt,
                    ApprovedPrice  = c.ApprovedPrice,
                })
                .ToListAsync();

            return Ok(new { total, page, items });
        }
    }
}
