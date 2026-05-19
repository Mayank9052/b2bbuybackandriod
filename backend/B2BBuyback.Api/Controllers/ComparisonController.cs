// FILE: Controllers/ComparisonController.cs
// Full rewrite supporting optional 3rd bike in all endpoints

using B2BBuyback.Api.Data;
using B2BBuyback.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using B2BBuyback.Api.DTOs;

namespace B2BBuyback.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComparisonController : ControllerBase
    {
        private readonly AppDbContext        _context;
        private readonly IWebHostEnvironment _environment;

        public ComparisonController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context     = context;
            _environment = environment;
        }

        // =============================================
        // GET /api/Comparison/list
        // Active comparisons for the list page
        // =============================================
        [HttpGet("list")]
        public async Task<IActionResult> GetComparisonList()
        {
            var configs = await _context.ComparisonConfigs
                .Where(c => c.IsActive)
                .Include(c => c.Scooty1).ThenInclude(s => s.Model)
                .Include(c => c.Scooty1).ThenInclude(s => s.Variant)
                .Include(c => c.Scooty2).ThenInclude(s => s.Model)
                .Include(c => c.Scooty2).ThenInclude(s => s.Variant)
                .Include(c => c.Scooty3!).ThenInclude(s => s.Model)
                .Include(c => c.Scooty3!).ThenInclude(s => s.Variant)
                .Select(c => new
                {
                    c.Id,
                    c.Scooty1Id,  c.Scooty2Id,  c.Scooty3Id,
                    Model1Name    = c.Scooty1.Model.ModelName,
                    Model2Name    = c.Scooty2.Model.ModelName,
                    Model3Name    = c.Scooty3 != null ? c.Scooty3.Model.ModelName : null,
                    Variant1Name  = c.Scooty1.Variant.VariantName,
                    Variant2Name  = c.Scooty2.Variant.VariantName,
                    Variant3Name  = c.Scooty3 != null ? c.Scooty3.Variant.VariantName : null,
                    Price1        = c.Scooty1.Price,
                    Price2        = c.Scooty2.Price,
                    Price3        = c.Scooty3 != null ? c.Scooty3.Price : null,
                    Image1Url     = c.Scooty1.ImageUrl,
                    Image2Url     = c.Scooty2.ImageUrl,
                    Image3Url     = c.Scooty3 != null ? c.Scooty3.ImageUrl : null,
                })
                .ToListAsync();

            return Ok(configs);
        }

        // =============================================
        // GET /api/Comparison/list-all
        // All comparisons (including inactive) — admin panel
        // =============================================
        [HttpGet("list-all")]
        public async Task<IActionResult> GetComparisonListAll()
        {
            var configs = await _context.ComparisonConfigs
                .Include(c => c.Scooty1).ThenInclude(s => s.Model)
                .Include(c => c.Scooty1).ThenInclude(s => s.Variant)
                .Include(c => c.Scooty2).ThenInclude(s => s.Model)
                .Include(c => c.Scooty2).ThenInclude(s => s.Variant)
                .Include(c => c.Scooty3!).ThenInclude(s => s.Model)
                .Include(c => c.Scooty3!).ThenInclude(s => s.Variant)
                .OrderByDescending(c => c.IsActive).ThenBy(c => c.Id)
                .Select(c => new
                {
                    c.Id,
                    c.Scooty1Id,  c.Scooty2Id,  c.Scooty3Id,
                    c.IsActive,
                    Model1Name    = c.Scooty1.Model.ModelName,
                    Model2Name    = c.Scooty2.Model.ModelName,
                    Model3Name    = c.Scooty3 != null ? c.Scooty3.Model.ModelName : null,
                    Variant1Name  = c.Scooty1.Variant.VariantName,
                    Variant2Name  = c.Scooty2.Variant.VariantName,
                    Variant3Name  = c.Scooty3 != null ? c.Scooty3.Variant.VariantName : null,
                    Price1        = c.Scooty1.Price,
                    Price2        = c.Scooty2.Price,
                    Price3        = c.Scooty3 != null ? c.Scooty3.Price : null,
                    Image1Url     = c.Scooty1.ImageUrl,
                    Image2Url     = c.Scooty2.ImageUrl,
                    Image3Url     = c.Scooty3 != null ? c.Scooty3.ImageUrl : null,
                })
                .ToListAsync();

            return Ok(configs);
        }

        // =============================================
        // GET /api/Comparison/{scootyId}
        // Full data for ONE scooty in the detail page
        // =============================================
        [HttpGet("{scootyId:int}")]
        public async Task<IActionResult> GetComparisonData(int scootyId)
        {
            var scooty = await _context.ScootyInventories
                .Include(s => s.Model)
                .Include(s => s.Variant)
                .Include(s => s.Colour)
                .Include(s => s.VehicleReviews)
                .Include(s => s.RoadPrices)
                .Include(s => s.ScootySpec)
                .FirstOrDefaultAsync(s => s.ScootyId == scootyId);

            if (scooty == null)
                return NotFound($"Scooty ID {scootyId} not found.");

            var avgRating   = scooty.VehicleReviews.Any()
                ? scooty.VehicleReviews.Average(r => r.Rating) : 0.0;
            var reviewCount = scooty.VehicleReviews.Count;
            var insurance   = scooty.RoadPrices.FirstOrDefault()?.InsuranceAmount;
            var exShowroom  = scooty.RoadPrices.FirstOrDefault()?.ExShowroomPrice;

            var brochure = await _context.VehicleBrochures
                .Where(b => b.ModelId == scooty.ModelId)
                .OrderByDescending(b => b.UploadedAt)
                .Select(b => b.BrochureUrl)
                .FirstOrDefaultAsync();

            var colours = await _context.VehicleColours
                .Where(c => c.ModelId == scooty.ModelId && c.VariantId == scooty.VariantId)
                .Select(c => new { c.ColourName, c.HexCode })
                .ToListAsync();

            var specs = scooty.ScootySpec;

            return Ok(new
            {
                scooty.ScootyId,
                ModelName        = scooty.Model.ModelName,
                VariantName      = scooty.Variant.VariantName,
                scooty.ImageUrl,
                scooty.Price,
                BrandName        = "BGauss",
                AvgRating        = Math.Round(avgRating, 1),
                ReviewCount      = reviewCount,
                ExShowroomPrice  = exShowroom,
                InsuranceAmount  = insurance,
                FuelType         = specs?.FuelType ?? "Electric",
                scooty.MaxPowerKw,
                scooty.RangeKm,
                scooty.ChargingTimeHrs,
                RidingModes      = specs?.RidingModes,
                ReverseMode      = specs?.ReverseMode ?? false,
                CruiseControl    = specs?.CruiseControl ?? false,
                scooty.BrakeFront,
                scooty.BrakeRear,
                scooty.BrakingType,
                scooty.WheelSize,
                scooty.WheelType,
                scooty.StartingType,
                scooty.Speedometer,
                UsbCharging      = specs?.UsbCharging ?? false,
                Colours          = colours,
                BrochureUrl      = brochure,
                BatteryWarranty  = specs?.BatteryWarranty,
                MotorWarranty    = specs?.MotorWarranty,
            });
        }

        // =============================================
        // GET /api/Comparison/variants-by-scooty/{scootyId}
        // All variants of the same model — for variant dropdown
        // =============================================
        [HttpGet("variants-by-scooty/{scootyId:int}")]
        public async Task<IActionResult> GetVariantsByScooty(int scootyId)
        {
            var scooty = await _context.ScootyInventories
                .Where(s => s.ScootyId == scootyId)
                .Select(s => new { s.ModelId })
                .FirstOrDefaultAsync();

            if (scooty == null) return NotFound();

            var variants = await _context.ScootyInventories
                .Include(s => s.Variant)
                .Where(s => s.ModelId == scooty.ModelId)
                .Select(s => new { s.ScootyId, VariantName = s.Variant.VariantName, s.Price })
                .ToListAsync();

            return Ok(variants);
        }

        // =============================================
        // POST /api/Comparison/config
        // Create comparison pair — supports 2 or 3 bikes
        // =============================================
        [HttpPost("config")]
        public async Task<IActionResult> CreateConfig([FromBody] CreateComparisonConfigDto dto)
        {
            if (!await _context.ScootyInventories.AnyAsync(s => s.ScootyId == dto.Scooty1Id))
                return BadRequest($"Scooty1 ID {dto.Scooty1Id} not found.");
            if (!await _context.ScootyInventories.AnyAsync(s => s.ScootyId == dto.Scooty2Id))
                return BadRequest($"Scooty2 ID {dto.Scooty2Id} not found.");
            if (dto.Scooty3Id.HasValue &&
                !await _context.ScootyInventories.AnyAsync(s => s.ScootyId == dto.Scooty3Id.Value))
                return BadRequest($"Scooty3 ID {dto.Scooty3Id} not found.");

            // Prevent exact duplicate pairs
            var exists = await _context.ComparisonConfigs.AnyAsync(c =>
                c.Scooty1Id == dto.Scooty1Id &&
                c.Scooty2Id == dto.Scooty2Id &&
                c.Scooty3Id == dto.Scooty3Id);

            if (exists) return BadRequest("This comparison already exists.");

            _context.ComparisonConfigs.Add(new ComparisonConfig
            {
                Scooty1Id = dto.Scooty1Id,
                Scooty2Id = dto.Scooty2Id,
                Scooty3Id = dto.Scooty3Id,   // nullable — null for 2-bike
                IsActive  = true,
            });

            await _context.SaveChangesAsync();
            return Ok("Comparison config created.");
        }

        // =============================================
        // PUT /api/Comparison/config/{id}/toggle
        // =============================================
        [HttpPut("config/{id:int}/toggle")]
        public async Task<IActionResult> ToggleConfig(int id)
        {
            var config = await _context.ComparisonConfigs.FindAsync(id);
            if (config == null) return NotFound();
            config.IsActive = !config.IsActive;
            await _context.SaveChangesAsync();
            return Ok(new { config.IsActive });
        }

        // =============================================
        // DELETE /api/Comparison/config/{id}
        // =============================================
        [HttpDelete("config/{id:int}")]
        public async Task<IActionResult> DeleteConfig(int id)
        {
            var config = await _context.ComparisonConfigs.FindAsync(id);
            if (config == null) return NotFound();
            _context.ComparisonConfigs.Remove(config);
            await _context.SaveChangesAsync();
            return Ok("Deleted.");
        }

        // =============================================
        // POST /api/Comparison/brochure/upload
        // =============================================
        [HttpPost("brochure/upload")]
        public async Task<IActionResult> UploadBrochure(
            [FromForm] int modelId, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");
            if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only PDF files are allowed.");
            if (!await _context.VehicleModels.AnyAsync(m => m.Id == modelId))
                return BadRequest($"Model ID {modelId} not found.");

            var rootPath = _environment.WebRootPath
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var folder   = Path.Combine(rootPath, "Brochures");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            var fileName = $"brochure_model_{modelId}_{Guid.NewGuid()}.pdf";
            var filePath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            var url = $"/Brochures/{fileName}";

            _context.VehicleBrochures.Add(new VehicleBrochure
            {
                ModelId    = modelId,
                BrochureUrl = url,
                UploadedAt  = DateTime.UtcNow,
            });

            await _context.SaveChangesAsync();
            return Ok(new { url });
        }
    }
}