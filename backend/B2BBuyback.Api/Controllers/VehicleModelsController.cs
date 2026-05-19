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
    public class VehicleModelsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VehicleModelsController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================
        // GET ALL
        // ==========================
        [HttpGet]
        public async Task<IActionResult> GetModels()
        {
            var models = await _context.VehicleModels.ToListAsync();
            return Ok(models);
        }

        // ==========================
        // GET BY ID
        // ==========================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetModel(int id)
        {
            var model = await _context.VehicleModels.FindAsync(id);

            if (model == null)
                return NotFound();

            return Ok(model);
        }

        // CREATE
        [HttpPost("CreateModel")]
        public async Task<IActionResult> CreateModel([FromBody] CreateModelDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.ModelName))
                    return BadRequest("ModelName is required");

                var model = new VehicleModel { ModelName = dto.ModelName.Trim() };
                _context.VehicleModels.Add(model);
                await _context.SaveChangesAsync();
                return Ok(model);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        // UPDATE
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateModel(int id, [FromBody] UpdateModelDto dto)
        {
            try
            {
                var existing = await _context.VehicleModels.FindAsync(id);
                if (existing == null) return NotFound();

                existing.ModelName = dto.ModelName.Trim();
                await _context.SaveChangesAsync();
                return Ok(existing);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        // ==========================
        // DELETE
        // ==========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteModel(int id)
        {
            var model = await _context.VehicleModels.FindAsync(id);

            if (model == null)
                return NotFound();

            _context.VehicleModels.Remove(model);
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
            var worksheet = package.Workbook.Worksheets.Add("VehicleModels");

            worksheet.Cells[1, 1].Value = "ModelName";

            var stream = new MemoryStream(package.GetAsByteArray());

            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "VehicleModelsTemplate.xlsx");
        }

        // ==========================
        // IMPORT EXCEL
        // ==========================
        [HttpPost("import")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var modelsToInsert = new List<VehicleModel>();
            int skippedCount = 0;

            try
            {
                // Fetch existing models from DB
                var existingModels = await _context.VehicleModels
                    .Where(x => x.ModelName != null)
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
                    var modelName = worksheet.Cells[row, 1].Text?.Trim();

                    if (string.IsNullOrWhiteSpace(modelName))
                        continue;

                    // Check existing model
                    var existingModel = existingModels
                        .FirstOrDefault(x => x.ModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase));

                    if (existingModel != null)
                    {
                        skippedCount++;
                        continue; // Skip duplicate
                    }

                    // Add new model
                    modelsToInsert.Add(new VehicleModel
                    {
                        ModelName = modelName
                    });
                }

                if (modelsToInsert.Any())
                    await _context.VehicleModels.AddRangeAsync(modelsToInsert);

                if (modelsToInsert.Any() || skippedCount > 0)
                    await _context.SaveChangesAsync();

                return Ok(new
                {
                    Inserted = modelsToInsert.Count,
                    Skipped = skippedCount,
                    Message = "Import completed"
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Import failed: {ex.Message}");
            }
        }
    }
}