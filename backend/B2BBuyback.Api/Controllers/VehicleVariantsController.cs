using B2BBuyback.Api.Data;
using B2BBuyback.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using B2BBuyback.Api.DTOs;

namespace B2BBuyback.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleVariantsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VehicleVariantsController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================
        // GET ALL VARIANTS
        // ==========================
        [HttpGet]
        public async Task<IActionResult> GetVariants()
        {
            var variants = await _context.VehicleVariants
                .Include(v => v.Model) // ✅ correct
                .Select(v => new
                {
                    v.Id,
                    v.VariantName,
                    v.ModelId,
                    ModelName = v.Model.ModelName // 🔥 send model name
                })
                .ToListAsync();

            return Ok(variants);
        }

        // ==========================
        // GET VARIANT BY ID
        // ==========================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVariant(int id)
        {
            var variant = await _context.VehicleVariants.FindAsync(id);

            if (variant == null)
                return NotFound();

            return Ok(variant);
        }

        // ==========================
        // CREATE VARIANT
        // ==========================
        [HttpPost("CreateVariant")]
        public async Task<IActionResult> CreateVariant([FromBody] CreateVariantDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("Data is null");

                if (string.IsNullOrWhiteSpace(dto.VariantName))
                    return BadRequest("VariantName is required");

                if (dto.ModelId == 0)
                    return BadRequest("ModelId is required");

                var variant = new VehicleVariant
                {
                    VariantName = dto.VariantName.Trim(),
                    ModelId     = dto.ModelId
                };

                _context.VehicleVariants.Add(variant);
                await _context.SaveChangesAsync();

                return Ok(variant);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }


        // ==========================
        // UPDATE VARIANT
        // ==========================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateVariant(int id, [FromBody] UpdateVariantDto dto)
        {
            try
            {
                if (id != dto.Id)
                    return BadRequest("ID mismatch");

                var existing = await _context.VehicleVariants.FindAsync(id);

                if (existing == null)
                    return NotFound();

                existing.VariantName = dto.VariantName.Trim();
                existing.ModelId     = dto.ModelId;

                await _context.SaveChangesAsync();

                return Ok(existing);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        // ==========================
        // DELETE VARIANT
        // ==========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVariant(int id)
        {
            var variant = await _context.VehicleVariants.FindAsync(id);

            if (variant == null)
                return NotFound();

            _context.VehicleVariants.Remove(variant);
            await _context.SaveChangesAsync();

            return Ok("Deleted successfully");
        }

        // ==========================
        // DOWNLOAD BLANK EXCEL
        // ==========================
        [HttpGet("download-template")]
        public IActionResult DownloadTemplate()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("VehicleVariants");

            worksheet.Cells[1, 1].Value = "VariantName";
            worksheet.Cells[1, 2].Value = "ModelId";

            var stream = new MemoryStream(package.GetAsByteArray());

            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "VehicleVariantsTemplate.xlsx");
        }

        // ==========================
        // IMPORT EXCEL
        // ==========================
        [HttpPost("import")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var variantsToInsert = new List<VehicleVariant>();
            int skippedCount = 0;

            try
            {
                // Get valid ModelIds
                var validModelIds = new HashSet<int>(
                    await _context.VehicleModels.Select(x => x.Id).ToListAsync()
                );

                // Get existing variants
                var existingVariants = await _context.VehicleVariants
                    .Where(v => v.VariantName != null && v.ModelId != null)
                    .ToListAsync();

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();

                if (worksheet == null)
                    return BadRequest("Invalid Excel file.");

                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    var variantName = worksheet.Cells[row, 1].Text?.Trim();

                    if (string.IsNullOrWhiteSpace(variantName))
                        continue;

                    int modelId = int.TryParse(worksheet.Cells[row, 2].Text, out var mId)
                        && validModelIds.Contains(mId) ? mId : 0;

                    if (modelId == 0)
                        continue;

                   // Check existing variant
                   var existingVariant = existingVariants
                        .FirstOrDefault(v =>
                            v.ModelId == modelId &&
                            string.Equals(v.VariantName, variantName, StringComparison.OrdinalIgnoreCase));

                    if (existingVariant != null)
                    {
                        skippedCount++;
                        continue;
                    }

                    // Add new variant
                    variantsToInsert.Add(new VehicleVariant
                    {
                        VariantName = variantName,
                        ModelId = modelId
                    });
                }

                if (variantsToInsert.Any())
                    await _context.VehicleVariants.AddRangeAsync(variantsToInsert);

                if (variantsToInsert.Any() || skippedCount > 0)
                    await _context.SaveChangesAsync();

                return Ok(new
                {
                    Inserted = variantsToInsert.Count,
                    Skipped = skippedCount,
                    Message = "Import completed successfully"
                });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message;
                return BadRequest($"Import failed: {ex.Message} | SQL Error: {inner}");
            }
        }
    }
}