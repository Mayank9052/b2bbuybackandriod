using B2BBuyback.Api.Data;
using B2BBuyback.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using B2BBuyback.Api.DTOs;

namespace B2BBuyback.Api.Controllers
{
    // =============================================
    // VEHICLE REVIEWS CONTROLLER
    // =============================================
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleReviewsController : ControllerBase
    {
        private readonly AppDbContext _context;
        public VehicleReviewsController(AppDbContext context) { _context = context; }

        // GET /api/VehicleReviews/{scootyId}
        // Returns all reviews + rating summary for a vehicle
        [HttpGet("{scootyId}")]
        public async Task<IActionResult> GetReviews(int scootyId)
        {
            var reviews = await _context.VehicleReviews
                .Where(r => r.ScootyId == scootyId && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.UserId,
                    r.Title,
                    r.ReviewText,
                    r.Rating,
                    r.PerformanceRating,
                    r.MileageRating,
                    r.ComfortRating,
                    r.MaintenanceRating,
                    r.FeaturesRating,
                    r.CreatedAt
                })
                .ToListAsync();

            // Build rating summary
            var summary = reviews.Count > 0 ? new
            {
                TotalReviews     = reviews.Count,
                AverageRating    = Math.Round(reviews.Average(r => r.Rating), 1),
                AvgPerformance   = reviews.Where(r => r.PerformanceRating.HasValue).Select(r => r.PerformanceRating!.Value).Any()
                                   ? Math.Round(reviews.Where(r => r.PerformanceRating.HasValue).Average(r => r.PerformanceRating!.Value), 1) : (double?)null,
                AvgMileage       = reviews.Where(r => r.MileageRating.HasValue).Select(r => r.MileageRating!.Value).Any()
                                   ? Math.Round(reviews.Where(r => r.MileageRating.HasValue).Average(r => r.MileageRating!.Value), 1) : (double?)null,
                AvgComfort       = reviews.Where(r => r.ComfortRating.HasValue).Select(r => r.ComfortRating!.Value).Any()
                                   ? Math.Round(reviews.Where(r => r.ComfortRating.HasValue).Average(r => r.ComfortRating!.Value), 1) : (double?)null,
                AvgMaintenance   = reviews.Where(r => r.MaintenanceRating.HasValue).Select(r => r.MaintenanceRating!.Value).Any()
                                   ? Math.Round(reviews.Where(r => r.MaintenanceRating.HasValue).Average(r => r.MaintenanceRating!.Value), 1) : (double?)null,
                AvgFeatures      = reviews.Where(r => r.FeaturesRating.HasValue).Select(r => r.FeaturesRating!.Value).Any()
                                   ? Math.Round(reviews.Where(r => r.FeaturesRating.HasValue).Average(r => r.FeaturesRating!.Value), 1) : (double?)null,
                RatingBreakdown  = new
                {
                    Five  = reviews.Count(r => r.Rating == 5),
                    Four  = reviews.Count(r => r.Rating == 4),
                    Three = reviews.Count(r => r.Rating == 3),
                    Two   = reviews.Count(r => r.Rating == 2),
                    One   = reviews.Count(r => r.Rating == 1),
                }
            } : null;

            return Ok(new { summary, reviews });
        }

        // POST /api/VehicleReviews
        // Submit a new review
        [HttpPost]
        public async Task<IActionResult> SubmitReview([FromBody] SubmitReviewDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.UserId))
                return BadRequest("userId required.");
            if (string.IsNullOrWhiteSpace(dto.Title) || dto.Title.Trim().Length < 5)
                return BadRequest("Title must be at least 5 characters.");
            if (string.IsNullOrWhiteSpace(dto.ReviewText) || dto.ReviewText.Trim().Length < 10)
                return BadRequest("Review must be at least 10 characters.");
            if (dto.Rating < 1 || dto.Rating > 5)
                return BadRequest("Rating must be 1–5.");

            var scootyExists = await _context.ScootyInventories.AnyAsync(s => s.ScootyId == dto.ScootyId);
            if (!scootyExists) return BadRequest("Vehicle not found.");

            // Check duplicate
            var alreadyReviewed = await _context.VehicleReviews
                .AnyAsync(r => r.ScootyId == dto.ScootyId && r.UserId == dto.UserId);
            if (alreadyReviewed)
                return BadRequest("You have already reviewed this vehicle.");

            _context.VehicleReviews.Add(new VehicleReview
            {
                ScootyId          = dto.ScootyId,
                UserId            = dto.UserId.Trim(),
                Title             = dto.Title.Trim(),
                ReviewText        = dto.ReviewText.Trim(),
                Rating            = dto.Rating,
                PerformanceRating = dto.PerformanceRating,
                MileageRating     = dto.MileageRating,
                ComfortRating     = dto.ComfortRating,
                MaintenanceRating = dto.MaintenanceRating,
                FeaturesRating    = dto.FeaturesRating,
                CreatedAt         = DateTime.UtcNow,
                IsApproved        = true
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Review submitted successfully!" });
        }

        // DELETE /api/VehicleReviews/{id}?userId=admin
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id, [FromQuery] string userId)
        {
            var review = await _context.VehicleReviews.FindAsync(id);
            if (review == null) return NotFound();
            if (review.UserId != userId) return Forbid();
            _context.VehicleReviews.Remove(review);
            await _context.SaveChangesAsync();
            return Ok("Deleted");
        }

        // GET /api/VehicleReviews/my?userId=admin
        [HttpGet("my")]
        public async Task<IActionResult> GetMyReviews([FromQuery] string userId)
        {
            var reviews = await _context.VehicleReviews
                .Where(r => r.UserId == userId)
                .Select(r => new { r.Id, r.ScootyId, r.Title, r.Rating, r.CreatedAt })
                .ToListAsync();
            return Ok(reviews);
        }
    }

}
