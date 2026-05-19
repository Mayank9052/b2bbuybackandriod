using B2BBuyback.Api.Data;
using B2BBuyback.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using B2BBuyback.Api.DTOs;

namespace B2BBuyback.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AreaScootyStockController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AreaScootyStockController(AppDbContext context) { _context = context; }

        // =============================================
        // GET ALL AREA STOCKS (admin)
        // GET /api/AreaScootyStock
        // =============================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.AreaScootyStocks
                .Include(s => s.Scooty).ThenInclude(x => x.Model)
                .Include(s => s.Scooty).ThenInclude(x => x.Variant)
                .Include(s => s.CityArea).ThenInclude(a => a.City)
                .OrderByDescending(s => s.UpdatedAt)
                .Select(s => new
                {
                    s.Id,
                    s.ScootyId,
                    ModelName   = s.Scooty.Model.ModelName,
                    VariantName = s.Scooty.Variant.VariantName,
                    s.CityAreaId,
                    AreaName    = s.CityArea.AreaName,
                    Pincode     = s.CityArea.Pincode,
                    CityName    = s.CityArea.City.CityName,
                    s.StockQuantity,
                    s.StockAvailable,
                    s.UpdatedAt
                })
                .ToListAsync();
            return Ok(data);
        }

        // =============================================
        // GET STOCKS FOR A SPECIFIC SCOOTY
        // GET /api/AreaScootyStock/scooty/{scootyId}
        // =============================================
        [HttpGet("scooty/{scootyId}")]
        public async Task<IActionResult> GetByScooty(int scootyId)
        {
            var data = await _context.AreaScootyStocks
                .Include(s => s.CityArea).ThenInclude(a => a.City)
                .Where(s => s.ScootyId == scootyId)
                .OrderBy(s => s.CityArea.City.CityName)
                .ThenBy(s => s.CityArea.AreaName)
                .Select(s => new
                {
                    s.Id,
                    s.CityAreaId,
                    AreaName     = s.CityArea.AreaName,
                    Pincode      = s.CityArea.Pincode,
                    CityName     = s.CityArea.City.CityName,
                    StateName    = s.CityArea.City.StateName,
                    s.StockQuantity,
                    s.StockAvailable,
                    s.UpdatedAt
                })
                .ToListAsync();
            return Ok(data);
        }

        // =============================================
        // GET STOCKS FOR A SPECIFIC CITY AREA
        // GET /api/AreaScootyStock/area/{areaId}
        // =============================================
        [HttpGet("area/{areaId}")]
        public async Task<IActionResult> GetByArea(int areaId)
        {
            var data = await _context.AreaScootyStocks
                .Include(s => s.Scooty).ThenInclude(x => x.Model)
                .Include(s => s.Scooty).ThenInclude(x => x.Variant)
                .Where(s => s.CityAreaId == areaId)
                .Select(s => new
                {
                    s.Id,
                    s.ScootyId,
                    ModelName    = s.Scooty.Model.ModelName,
                    VariantName  = s.Scooty.Variant.VariantName,
                    s.Scooty.ImageUrl,
                    s.Scooty.Price,
                    s.Scooty.RangeKm,
                    s.StockQuantity,
                    s.StockAvailable,
                    s.UpdatedAt
                })
                .OrderByDescending(s => s.StockAvailable)
                .ThenBy(s => s.ModelName)
                .ToListAsync();
            return Ok(data);
        }

        // =============================================
        // UPSERT (ADD / UPDATE) AREA STOCK
        // POST /api/AreaScootyStock/upsert
        // Body: { scootyId, cityAreaId, stockQuantity }
        // =============================================
        [HttpPost("upsert")]
        public async Task<IActionResult> Upsert([FromBody] UpsertAreaStockDto dto)
        {
            if (dto.StockQuantity < 0)
                return BadRequest("Stock quantity cannot be negative.");

            var scootyExists = await _context.ScootyInventories.AnyAsync(s => s.ScootyId == dto.ScootyId);
            if (!scootyExists) return BadRequest($"Scooty ID {dto.ScootyId} not found.");

            var areaExists = await _context.CityAreas.AnyAsync(a => a.Id == dto.CityAreaId);
            if (!areaExists) return BadRequest($"City Area ID {dto.CityAreaId} not found.");

            var existing = await _context.AreaScootyStocks
                .FirstOrDefaultAsync(s => s.ScootyId == dto.ScootyId && s.CityAreaId == dto.CityAreaId);

            if (existing != null)
            {
                existing.StockQuantity = dto.StockQuantity;
                existing.UpdatedAt     = DateTime.UtcNow;
            }
            else
            {
                _context.AreaScootyStocks.Add(new AreaScootyStock
                {
                    ScootyId      = dto.ScootyId,
                    CityAreaId    = dto.CityAreaId,
                    StockQuantity = dto.StockQuantity,
                    UpdatedAt     = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            // Also update global stock on ScootyInventory:
            // If any area has stock > 0, mark global as available
            await SyncGlobalStock(dto.ScootyId);

            return Ok(new { message = "Area stock updated." });
        }

        // =============================================
        // BULK UPSERT — multiple areas at once
        // POST /api/AreaScootyStock/bulk-upsert
        // Body: { scootyId, stocks: [{ cityAreaId, stockQuantity }] }
        // =============================================
        [HttpPost("bulk-upsert")]
        public async Task<IActionResult> BulkUpsert([FromBody] BulkUpsertDto dto)
        {
            if (dto.Stocks == null || dto.Stocks.Count == 0)
                return BadRequest("No stock data provided.");

            var scootyExists = await _context.ScootyInventories.AnyAsync(s => s.ScootyId == dto.ScootyId);
            if (!scootyExists) return BadRequest($"Scooty ID {dto.ScootyId} not found.");

            var existingStocks = await _context.AreaScootyStocks
                .Where(s => s.ScootyId == dto.ScootyId)
                .ToListAsync();

            foreach (var item in dto.Stocks)
            {
                if (item.StockQuantity < 0) continue;

                var existing = existingStocks.FirstOrDefault(s => s.CityAreaId == item.CityAreaId);
                if (existing != null)
                {
                    existing.StockQuantity = item.StockQuantity;
                    existing.UpdatedAt     = DateTime.UtcNow;
                }
                else
                {
                    _context.AreaScootyStocks.Add(new AreaScootyStock
                    {
                        ScootyId      = dto.ScootyId,
                        CityAreaId    = item.CityAreaId,
                        StockQuantity = item.StockQuantity,
                        UpdatedAt     = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();
            await SyncGlobalStock(dto.ScootyId);

            return Ok(new { message = $"Bulk stock updated for Scooty {dto.ScootyId}." });
        }

        // =============================================
        // ADJUST STOCK (increment/decrement)
        // PATCH /api/AreaScootyStock/{id}/adjust
        // Body: { delta: 1 or -1 }
        // =============================================
        [HttpPatch("{id}/adjust")]
        public async Task<IActionResult> AdjustStock(int id, [FromBody] AdjustStockDto dto)
        {
            var stock = await _context.AreaScootyStocks.FindAsync(id);
            if (stock == null) return NotFound();

            var newQty = stock.StockQuantity + dto.Delta;
            if (newQty < 0) return BadRequest("Stock cannot go below 0.");

            stock.StockQuantity = newQty;
            stock.UpdatedAt     = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await SyncGlobalStock(stock.ScootyId);

            return Ok(new { message = "Stock adjusted.", newQuantity = newQty });
        }

        // =============================================
        // DELETE
        // =============================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var stock = await _context.AreaScootyStocks.FindAsync(id);
            if (stock == null) return NotFound();
            int scootyId = stock.ScootyId;
            _context.AreaScootyStocks.Remove(stock);
            await _context.SaveChangesAsync();
            await SyncGlobalStock(scootyId);
            return Ok("Deleted");
        }

        // ── Private: sync global StockAvailable/StockQuantity ──
        private async Task SyncGlobalStock(int scootyId)
        {
            var scooty = await _context.ScootyInventories.FindAsync(scootyId);
            if (scooty == null) return;

            var totalQty = await _context.AreaScootyStocks
                .Where(s => s.ScootyId == scootyId)
                .SumAsync(s => s.StockQuantity);

            scooty.StockQuantity  = totalQty;
            scooty.StockAvailable = totalQty > 0;
            await _context.SaveChangesAsync();
        }
    }
}