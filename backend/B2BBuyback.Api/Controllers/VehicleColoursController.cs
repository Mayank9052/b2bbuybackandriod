using B2BBuyback.Api.Data;
using B2BBuyback.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using B2BBuyback.Api.DTOs;
using System.Security.Cryptography;

namespace B2BBuyback.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleColoursController : ControllerBase
    {
        private readonly AppDbContext _context;

        public VehicleColoursController(AppDbContext context)
        {
            _context = context;
        }

        // ==========================
        // GET ALL COLOURS
        // ==========================
        [HttpGet]
        public async Task<IActionResult> GetColours()
        {
            var colours = await _context.VehicleColours.ToListAsync();
            return Ok(colours);
        }

        // ==========================
        // GET COLOUR BY ID
        // ==========================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetColour(int id)
        {
            var colour = await _context.VehicleColours.FindAsync(id);
            if (colour == null) return NotFound();
            return Ok(colour);
        }

        // ==========================
        // CREATE
        // ==========================
        [HttpPost("CreateColour")]
        public async Task<IActionResult> CreateColour([FromBody] CreateColourDto dto)
        {
            try
            {
                if (dto == null) return BadRequest("Data is null");
                if (string.IsNullOrWhiteSpace(dto.ColourName)) return BadRequest("ColourName required");
                if (dto.ModelId == 0) return BadRequest("ModelId required");
                if (dto.VariantId == 0) return BadRequest("VariantId required");

                var colour = new VehicleColour
                {
                    ColourName = dto.ColourName.Trim(),
                    ModelId = dto.ModelId,
                    VariantId = dto.VariantId,
                    HexCode = GenerateHexCode(dto.ColourName) // Generate Hex Code
                };

                _context.VehicleColours.Add(colour);
                await _context.SaveChangesAsync();
                return Ok(colour);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        // ==========================
        // UPDATE
        // ==========================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateColour(int id, [FromBody] UpdateColourDto dto)
        {
            try
            {
                if (id != dto.Id) return BadRequest("ID mismatch");

                var existing = await _context.VehicleColours.FindAsync(id);
                if (existing == null) return NotFound();

                existing.ColourName = dto.ColourName.Trim();
                existing.ModelId = dto.ModelId;
                existing.VariantId = dto.VariantId;
                existing.HexCode = GenerateHexCode(dto.ColourName); // Update Hex Code

                await _context.SaveChangesAsync();
                return Ok(existing);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.InnerException?.Message ?? ex.Message);
            }
        }

        // ==========================
        // DELETE COLOUR
        // ==========================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteColour(int id)
        {
            var colour = await _context.VehicleColours.FindAsync(id);
            if (colour == null) return NotFound();

            _context.VehicleColours.Remove(colour);
            await _context.SaveChangesAsync();
            return Ok("Deleted successfully");
        }

        // ==========================
        // DOWNLOAD BLANK EXCEL TEMPLATE
        // ==========================
        [HttpGet("download-template")]
        public IActionResult DownloadTemplate()
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("VehicleColours");

            worksheet.Cells[1, 1].Value = "ColourName";
            worksheet.Cells[1, 2].Value = "ModelId";
            worksheet.Cells[1, 3].Value = "VariantId";
            worksheet.Cells[1, 4].Value = "HexCode"; // Added HexCode column

            var stream = new MemoryStream(package.GetAsByteArray());
            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "VehicleColoursTemplate.xlsx");
        }

        // ==========================
        // IMPORT EXCEL
        // ==========================
        [HttpPost("import")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var coloursToInsert = new List<VehicleColour>();
            int updatedCount = 0;
            var skippedRows = new List<int>();

            try
            {
                var existingColours = await _context.VehicleColours.ToListAsync();

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);
                stream.Position = 0;

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) return BadRequest("Invalid Excel file.");

                int rowCount = worksheet.Dimension.Rows;

                for (int row = 2; row <= rowCount; row++)
                {
                    var colourName = worksheet.Cells[row, 1].Text?.Trim();
                    if (string.IsNullOrWhiteSpace(colourName))
                    {
                        skippedRows.Add(row);
                        continue;
                    }

                    int? modelId = int.TryParse(worksheet.Cells[row, 2].Text, out var mId) ? mId : (int?)null;
                    int? variantId = int.TryParse(worksheet.Cells[row, 3].Text, out var vId) ? vId : (int?)null;

                    var existingColour = existingColours.FirstOrDefault(c =>
                        c.ColourName!.Equals(colourName, StringComparison.OrdinalIgnoreCase) &&
                        c.ModelId == modelId &&
                        c.VariantId == variantId
                    );

                    string hexCode = GenerateHexCode(colourName); // Generate HexCode

                    if (existingColour != null)
                    {
                        // Update HexCode if needed
                        existingColour.HexCode = hexCode;
                        updatedCount++;
                        continue;
                    }

                    coloursToInsert.Add(new VehicleColour
                    {
                        ColourName = colourName,
                        ModelId = modelId,
                        VariantId = variantId,
                        HexCode = hexCode
                    });
                }

                if (coloursToInsert.Any()) await _context.VehicleColours.AddRangeAsync(coloursToInsert);
                if (coloursToInsert.Any() || updatedCount > 0) await _context.SaveChangesAsync();

                var message = $"{coloursToInsert.Count} inserted, {updatedCount} updated.";
                if (skippedRows.Any()) message += $" Skipped rows: {string.Join(",", skippedRows)}";

                return Ok(message);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message;
                return BadRequest($"Import failed: {ex.Message} | SQL Error: {inner}");
            }
        }

        // ==========================
        // HEX CODE GENERATOR
        // ==========================
        private string GenerateHexCode(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "#000000";

            // Use hash to create deterministic colour
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            // Take first 3 bytes and convert to hex
            return $"#{hash[0]:X2}{hash[1]:X2}{hash[2]:X2}";
        }
    }
}