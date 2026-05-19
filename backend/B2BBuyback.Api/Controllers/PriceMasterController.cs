using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using B2BBuyback.Api.Data;
using B2BBuyback.Api.Models;

namespace B2BBuyback.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PriceMasterController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PriceMasterController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // =============================
        // GET ALL
        // =============================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.PriceMasters
                .Include(x => x.Model)
                .Include(x => x.Variant)
                .ToListAsync();

            return Ok(data);
        }

        // =============================
        // GET BY ID
        // =============================
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var data = await _context.PriceMasters.FindAsync(id);

            if (data == null)
                return NotFound();

            return Ok(data);
        }

        // =============================
        // ADD PRICE
        // =============================
        [HttpPost("add")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Add(
            [FromForm] int modelId,
            [FromForm] int variantId,
            [FromForm] DateOnly? effectiveStartDate,
            [FromForm] DateOnly? effectiveEndDate,
            [FromForm] decimal price,
            [FromForm] IFormFile? priceListFile)
        {
            try
            {
                if (effectiveStartDate == null)
                    return BadRequest("EffectiveStartDate is required.");

                // Handle file upload
                string? filePath = null;
                if (priceListFile != null && priceListFile.Length > 0)
                {
                    var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string folder = Path.Combine(rootPath, "PriceListFiles");

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    string fileName = Guid.NewGuid() + Path.GetExtension(priceListFile.FileName);
                    string fullPath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(fullPath, FileMode.Create))
                    {
                        await priceListFile.CopyToAsync(stream);
                    }

                    filePath = "/PriceListFiles/" + fileName;
                }

                var data = new PriceMaster
                {
                    ModelId = modelId,
                    VariantId = variantId,
                    EffectiveStartDate = effectiveStartDate.Value,
                    EffectiveEndDate = effectiveEndDate,
                    Price = price,
                    PriceListFile = filePath
                };

                _context.PriceMasters.Add(data);
                await _context.SaveChangesAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =============================
        // UPDATE
        // =============================
        [HttpPut("{id}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Update(
            int id,
            [FromForm] int modelId,
            [FromForm] int variantId,
            [FromForm] DateOnly? effectiveStartDate,   // nullable
            [FromForm] DateOnly? effectiveEndDate,     // nullable
            [FromForm] decimal price,
            [FromForm] IFormFile? priceListFile)
        {
            var existing = await _context.PriceMasters.FindAsync(id);

            if (existing == null)
                return NotFound();

            // Only update start date if provided
            if (effectiveStartDate.HasValue)
                existing.EffectiveStartDate = effectiveStartDate.Value;

            // End date can be null
            existing.EffectiveEndDate = effectiveEndDate;

            existing.ModelId = modelId;
            existing.VariantId = variantId;
            existing.Price = price;

            // File upload update
            if (priceListFile != null && priceListFile.Length > 0)
            {
                var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string folder = Path.Combine(rootPath, "PriceListFiles");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(priceListFile.FileName);
                string fullPath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await priceListFile.CopyToAsync(stream);
                }

                existing.PriceListFile = "/PriceListFiles/" + fileName;
            }

            await _context.SaveChangesAsync();

            return Ok(existing);
        }

        // =============================
        // DELETE
        // =============================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var data = await _context.PriceMasters.FindAsync(id);

            if (data == null)
                return NotFound();

            _context.PriceMasters.Remove(data);
            await _context.SaveChangesAsync();

            return Ok("Deleted successfully");
        }

        // =============================
        // DOWNLOAD BLANK EXCEL
        // =============================
        [HttpGet("download-template")]
        public IActionResult DownloadTemplate()
        {
            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("PriceMaster");

            sheet.Cells[1, 1].Value = "ModelId";
            sheet.Cells[1, 2].Value = "VariantId";
            sheet.Cells[1, 3].Value = "EffectiveStartDate";
            sheet.Cells[1, 4].Value = "EffectiveEndDate";
            sheet.Cells[1, 5].Value = "Price";

            var stream = new MemoryStream(package.GetAsByteArray());

            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "PriceMasterTemplate.xlsx");
        }

        // =============================
        // IMPORT EXCEL
        // =============================
        [HttpPost("import")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var priceToInsert = new List<PriceMaster>();
            int updatedCount = 0;
            var skippedRows = new List<int>();

            var existingPrices = await _context.PriceMasters.ToListAsync();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var sheet = package.Workbook.Worksheets.FirstOrDefault();

            if (sheet == null)
                return BadRequest("Invalid Excel file.");

            int rowCount = sheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                var modelText = sheet.Cells[row, 1].Text;
                var variantText = sheet.Cells[row, 2].Text;
                var startDateText = sheet.Cells[row, 3].Text;
                var endDateText = sheet.Cells[row, 4].Text;
                var priceText = sheet.Cells[row, 5].Text;

                if (!int.TryParse(modelText, out int modelId) ||
                    !int.TryParse(variantText, out int variantId) ||
                    !decimal.TryParse(priceText, out decimal price))
                {
                    skippedRows.Add(row);
                    continue;
                }

                DateOnly startDate;
                try
                {
                    startDate = DateOnly.Parse(startDateText);
                }
                catch
                {
                    skippedRows.Add(row);
                    continue;
                }

                DateOnly? endDate = string.IsNullOrWhiteSpace(endDateText)
                    ? null
                    : DateOnly.Parse(endDateText);

                var existing = existingPrices.FirstOrDefault(x =>
                    x.ModelId == modelId &&
                    x.VariantId == variantId &&
                    x.EffectiveStartDate == startDate);

                if (existing != null)
                {
                    existing.Price = price;
                    existing.EffectiveEndDate = endDate;
                    updatedCount++;
                    continue;
                }

                priceToInsert.Add(new PriceMaster
                {
                    ModelId = modelId,
                    VariantId = variantId,
                    EffectiveStartDate = startDate,
                    EffectiveEndDate = endDate,
                    Price = price
                });
            }

            if (priceToInsert.Any())
                await _context.PriceMasters.AddRangeAsync(priceToInsert);

            if (priceToInsert.Any() || updatedCount > 0)
                await _context.SaveChangesAsync();

            var message = $"{priceToInsert.Count} inserted, {updatedCount} updated.";
            if (skippedRows.Any())
                message += $" Skipped rows: {string.Join(",", skippedRows)}";

            return Ok(message);
        }
    }
}