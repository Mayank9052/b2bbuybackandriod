using B2BBuyback.Api.Data;
using B2BBuyback.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace B2BBuyback.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserLikesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserLikesController(AppDbContext context)
        {
            _context = context;
        }

        // ==============================
        // TOGGLE LIKE
        // POST /api/UserLikes/{scootyId}?userId=admin
        // ==============================
        [HttpPost("{scootyId}")]
        public async Task<IActionResult> ToggleLike(int scootyId, [FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("userId is required");

            try
            {
                var existing = await _context.UserLikes
                    .FirstOrDefaultAsync(x => x.ScootyId == scootyId && x.UserId == userId);

                if (existing != null)
                {
                    // Already liked → unlike
                    _context.UserLikes.Remove(existing);
                    await _context.SaveChangesAsync();

                    var newCount = await _context.UserLikes.CountAsync(x => x.ScootyId == scootyId);
                    return Ok(new { liked = false, count = newCount });
                }
                else
                {
                    // Not liked → like
                    _context.UserLikes.Add(new UserLike
                    {
                        ScootyId = scootyId,
                        UserId   = userId,
                        LikedAt  = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();

                    var newCount = await _context.UserLikes.CountAsync(x => x.ScootyId == scootyId);
                    return Ok(new { liked = true, count = newCount });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        // ==============================
        // CHECK IF LIKED
        // GET /api/UserLikes/{scootyId}?userId=admin
        // ==============================
        [HttpGet("{scootyId}")]
        public async Task<IActionResult> IsLiked(int scootyId, [FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("userId is required");

            var liked = await _context.UserLikes
                .AnyAsync(x => x.ScootyId == scootyId && x.UserId == userId);

            var count = await _context.UserLikes
                .CountAsync(x => x.ScootyId == scootyId);

            return Ok(new { liked, count });
        }

        // ==============================
        // GET ALL LIKED VEHICLES FOR A USER
        // GET /api/UserLikes/my?userId=admin
        // ==============================
        [HttpGet("my")]
        public async Task<IActionResult> GetMyLikes([FromQuery] string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return BadRequest("userId is required");

            var likes = await _context.UserLikes
                .Where(x => x.UserId == userId)
                .Select(x => x.ScootyId)
                .ToListAsync();

            return Ok(likes);
        }

        // ==============================
        // GET LIKE COUNT FOR A VEHICLE
        // GET /api/UserLikes/{scootyId}/count
        // ==============================
        [HttpGet("{scootyId}/count")]
        public async Task<IActionResult> GetLikeCount(int scootyId)
        {
            var count = await _context.UserLikes
                .CountAsync(x => x.ScootyId == scootyId);

            return Ok(new { count });
        }

        // ==============================
        // GET TOP LIKED VEHICLES
        // GET /api/UserLikes/top
        // ==============================
        [HttpGet("top")]
        public async Task<IActionResult> GetTopLiked()
        {
            var top = await _context.UserLikes
                .GroupBy(x => x.ScootyId)
                .Select(g => new { scootyId = g.Key, count = g.Count() })
                .OrderByDescending(x => x.count)
                .Take(10)
                .ToListAsync();

            return Ok(top);
        }
    }
}