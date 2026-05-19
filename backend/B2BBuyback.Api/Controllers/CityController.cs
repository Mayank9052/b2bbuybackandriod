using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using B2BBuyback.Api.Data;
using B2BBuyback.Api.Models;
using B2BBuyback.Api.DTOs;

namespace B2BBuyback.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CityController : ControllerBase
    {
        private readonly AppDbContext _context;
        public CityController(AppDbContext context) { _context = context; }

        // =============================================
        // GET ALL CITIES
        // =============================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var cities = await _context.Cities
                .OrderByDescending(c => c.IsPopular)
                .ThenBy(c => c.CityName)
                .Select(c => new
                {
                    c.Id, c.CityName, c.StateName,
                    c.IsPopular, c.PincodePrefix,
                    AreaCount = c.CityAreas.Count
                })
                .ToListAsync();
            return Ok(cities);
        }

        // =============================================
        // GET CITY BY ID with areas
        // =============================================
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var city = await _context.Cities
                .Include(c => c.CityAreas)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (city == null) return NotFound();

            return Ok(new
            {
                city.Id, city.CityName, city.StateName,
                city.IsPopular, city.PincodePrefix,
                Areas = city.CityAreas.OrderBy(a => a.AreaName)
                    .Select(a => new { a.Id, a.AreaName, a.Pincode })
                    .ToList()
            });
        }

        // =============================================
        // GET AREAS BY CITY
        // =============================================
        [HttpGet("{id}/areas")]
        public async Task<IActionResult> GetAreas(int id)
        {
            var areas = await _context.CityAreas
                .Where(a => a.CityId == id)
                .OrderBy(a => a.AreaName)
                .Select(a => new { a.Id, a.AreaName, a.Pincode })
                .ToListAsync();
            return Ok(areas);
        }

        // =============================================
        // SEARCH BY PINCODE OR CITY NAME
        // GET /api/City/search?q=400069   (pincode)
        // GET /api/City/search?q=Mumbai   (city name)
        // Returns matching areas with city info
        // =============================================
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return BadRequest("Enter at least 2 characters.");

            var cleanQ = q.Trim();
            bool isNumeric = cleanQ.All(char.IsDigit);

            IQueryable<CityArea> query = _context.CityAreas.Include(a => a.City);

            if (isNumeric)
            {
                // Pincode search: prefix match
                query = query.Where(a => a.Pincode.StartsWith(cleanQ));
            }
            else
            {
                // City name or area name search
                query = query.Where(a =>
                    a.City.CityName.ToLower().Contains(cleanQ.ToLower()) ||
                    a.AreaName.ToLower().Contains(cleanQ.ToLower()) ||
                    a.City.StateName.ToLower().Contains(cleanQ.ToLower())
                );
            }

            var results = await query
                .OrderBy(a => a.Pincode)
                .Select(a => new
                {
                    a.Id,
                    a.AreaName,
                    a.Pincode,
                    CityId    = a.City.Id,
                    CityName  = a.City.CityName,
                    StateName = a.City.StateName
                })
                .Take(15)
                .ToListAsync();

            return Ok(results);
        }

        // =============================================
        // LEGACY: search-pincode (kept for compatibility)
        // =============================================
        [HttpGet("search-pincode")]
        public async Task<IActionResult> SearchByPincode([FromQuery] string pincode)
            => await Search(pincode);

        // =============================================
        // GET VEHICLES FOR PINCODE / CITY AREA
        // GET /api/City/pincode-vehicles?pincode=400069
        // GET /api/City/pincode-vehicles?cityId=2
        //
        // Logic:
        //  1. Find city area from pincode (exact or prefix)
        //  2. Return scooties that have AreaScootyStock > 0 for that area
        //  3. If no area-stock records, fall back to global in-stock scooties
        // =============================================
        [HttpGet("pincode-vehicles")]
        public async Task<IActionResult> GetVehiclesByPincode(
            [FromQuery] string? pincode,
            [FromQuery] int? cityId)
        {
            // ── Resolve area / city ───────────────────
            CityArea? area  = null;
            City?     city  = null;

            if (!string.IsNullOrWhiteSpace(pincode))
            {
                var cleanPin = pincode.Trim();

                // Exact match first
                area = await _context.CityAreas
                    .Include(a => a.City)
                    .FirstOrDefaultAsync(a => a.Pincode == cleanPin);

                // Prefix match fallback (3 digits)
                if (area == null && cleanPin.Length >= 3)
                    area = await _context.CityAreas
                        .Include(a => a.City)
                        .FirstOrDefaultAsync(a => a.Pincode.StartsWith(cleanPin.Substring(0, 3)));

                city = area?.City;
            }
            else if (cityId.HasValue)
            {
                city = await _context.Cities.FindAsync(cityId.Value);
            }

            // ── Try area-specific stock first ─────────
            List<object> vehicles = new();
            bool usedAreaStock = false;

            if (area != null)
            {
                var areaStock = await _context.AreaScootyStocks
                    .Include(s => s.Scooty).ThenInclude(x => x.Model)
                    .Include(s => s.Scooty).ThenInclude(x => x.Variant)
                    .Where(s => s.CityAreaId == area.Id && s.StockQuantity > 0)
                    .ToListAsync();

                if (areaStock.Count > 0)
                {
                    usedAreaStock = true;
                    // Group by model, show one per model
                    vehicles = areaStock
                        .GroupBy(s => s.Scooty.ModelId)
                        .Select(g => g.OrderByDescending(s => s.ScootyId).First())
                        .Select(s => (object)new
                        {
                            s.ScootyId,
                            ModelName    = s.Scooty.Model.ModelName,
                            VariantName  = s.Scooty.Variant.VariantName,
                            s.Scooty.ImageUrl,
                            StockAvailable = s.StockAvailable,
                            StockQuantity  = s.StockQuantity,
                            Price        = s.Scooty.Price,
                            RangeKm      = s.Scooty.RangeKm
                        })
                        .ToList();
                }
            }

            // ── City-wide stock fallback ───────────────
            if (!usedAreaStock && city != null)
            {
                var cityAreaIds = await _context.CityAreas
                    .Where(a => a.CityId == city.Id)
                    .Select(a => a.Id)
                    .ToListAsync();

                if (cityAreaIds.Count > 0)
                {
                    var cityStock = await _context.AreaScootyStocks
                        .Include(s => s.Scooty).ThenInclude(x => x.Model)
                        .Include(s => s.Scooty).ThenInclude(x => x.Variant)
                        .Where(s => cityAreaIds.Contains(s.CityAreaId) && s.StockQuantity > 0)
                        .ToListAsync();

                    if (cityStock.Count > 0)
                    {
                        usedAreaStock = true;
                        vehicles = cityStock
                            .GroupBy(s => s.Scooty.ModelId)
                            .Select(g => g.OrderByDescending(s => s.ScootyId).First())
                            .Select(s => (object)new
                            {
                                s.ScootyId,
                                ModelName    = s.Scooty.Model.ModelName,
                                VariantName  = s.Scooty.Variant.VariantName,
                                s.Scooty.ImageUrl,
                                StockAvailable = s.StockAvailable,
                                StockQuantity  = s.StockQuantity,
                                Price        = s.Scooty.Price,
                                RangeKm      = s.Scooty.RangeKm
                            })
                            .ToList();
                    }
                }
            }

            // ── Global fallback: all in-stock scooties ─
            if (!usedAreaStock)
            {
                var globalVehicles = await _context.ScootyInventories
                    .Include(x => x.Model)
                    .Include(x => x.Variant)
                    .Where(x => x.ImageUrl != null && x.StockAvailable)
                    .GroupBy(x => x.ModelId)
                    .Select(g => g.OrderByDescending(x => x.ScootyId)
                        .Select(x => new
                        {
                            x.ScootyId,
                            ModelName    = x.Model.ModelName,
                            VariantName  = x.Variant.VariantName,
                            x.ImageUrl,
                            x.StockAvailable,
                            StockQuantity = x.StockQuantity,
                            x.Price,
                            x.RangeKm
                        })
                        .FirstOrDefault()
                    )
                    .ToListAsync();

                vehicles = globalVehicles.Select(v => (object)v!).ToList();
            }

            return Ok(new
            {
                pincode      = pincode,
                areaId       = area?.Id,
                areaName     = area?.AreaName,
                cityId       = city?.Id,
                cityName     = city?.CityName,
                stateName    = city?.StateName,
                usedAreaStock,
                vehicleCount = vehicles.Count,
                vehicles
            });
        }

        // =============================================
        // ADD CITY
        // =============================================
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] AddCityRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.CityName))
                return BadRequest("City name is required.");
            if (await _context.Cities.AnyAsync(x => x.CityName == model.CityName.Trim()))
                return BadRequest("City already exists.");

            var city = new City
            {
                CityName      = model.CityName.Trim(),
                StateName     = model.StateName?.Trim() ?? "",
                IsPopular     = model.IsPopular,
                PincodePrefix = model.PincodePrefix?.Trim()
            };
            _context.Cities.Add(city);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(model.PincodePrefix) &&
                (model.Areas == null || model.Areas.Count == 0))
            {
                _context.CityAreas.Add(new CityArea
                {
                    CityId   = city.Id,
                    AreaName = $"{model.CityName} Central",
                    Pincode  = model.PincodePrefix + "001"
                });
                await _context.SaveChangesAsync();
            }

            if (model.Areas?.Count > 0)
            {
                foreach (var area in model.Areas)
                {
                    if (string.IsNullOrWhiteSpace(area.AreaName) || string.IsNullOrWhiteSpace(area.Pincode)) continue;
                    if (await _context.CityAreas.AnyAsync(a => a.Pincode == area.Pincode.Trim())) continue;
                    _context.CityAreas.Add(new CityArea
                    {
                        CityId   = city.Id,
                        AreaName = area.AreaName.Trim(),
                        Pincode  = area.Pincode.Trim()
                    });
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "City added.", cityId = city.Id });
        }

        // =============================================
        // ADD AREA TO CITY
        // =============================================
        [HttpPost("{cityId}/areas")]
        public async Task<IActionResult> AddArea(int cityId, [FromBody] AddAreaRequest area)
        {
            if (!await _context.Cities.AnyAsync(c => c.Id == cityId))
                return NotFound("City not found.");
            if (string.IsNullOrWhiteSpace(area.AreaName)) return BadRequest("Area name required.");
            if (string.IsNullOrWhiteSpace(area.Pincode))  return BadRequest("Pincode required.");
            if (await _context.CityAreas.AnyAsync(a => a.Pincode == area.Pincode.Trim()))
                return BadRequest($"Pincode {area.Pincode} already exists.");

            _context.CityAreas.Add(new CityArea
            {
                CityId   = cityId,
                AreaName = area.AreaName.Trim(),
                Pincode  = area.Pincode.Trim()
            });
            await _context.SaveChangesAsync();
            return Ok(new { message = "Area added." });
        }

        // =============================================
        // DELETE AREA
        // =============================================
        [HttpDelete("areas/{areaId}")]
        public async Task<IActionResult> DeleteArea(int areaId)
        {
            var area = await _context.CityAreas.FindAsync(areaId);
            if (area == null) return NotFound();
            _context.CityAreas.Remove(area);
            await _context.SaveChangesAsync();
            return Ok("Deleted");
        }

        // =============================================
        // UPDATE / DELETE CITY
        // =============================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] AddCityRequest model)
        {
            var existing = await _context.Cities.FindAsync(id);
            if (existing == null) return NotFound();
            existing.CityName      = model.CityName.Trim();
            existing.StateName     = model.StateName?.Trim() ?? "";
            existing.IsPopular     = model.IsPopular;
            existing.PincodePrefix = model.PincodePrefix?.Trim();
            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var city = await _context.Cities.FindAsync(id);
            if (city == null) return NotFound();
            _context.Cities.Remove(city);
            await _context.SaveChangesAsync();
            return Ok("Deleted");
        }

        // =============================================
        // DOWNLOAD TEMPLATE / IMPORT EXCEL
        // =============================================
        [HttpGet("download-template")]
        public IActionResult DownloadTemplate()
        {
            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("Cities");
            sheet.Cells[1, 1].Value = "CityName";
            sheet.Cells[1, 2].Value = "StateName";
            sheet.Cells[1, 3].Value = "IsPopular (true/false)";
            sheet.Cells[1, 4].Value = "PincodePrefix";
            return File(new MemoryStream(package.GetAsByteArray()),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "CityTemplate.xlsx");
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("No file uploaded.");
            var toInsert = new List<City>(); int updatedCount = 0;
            var skippedRows = new List<int>();
            var existingCities = await _context.Cities.ToListAsync();
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream); stream.Position = 0;
            using var package = new ExcelPackage(stream);
            var sheet = package.Workbook.Worksheets.FirstOrDefault();
            if (sheet == null) return BadRequest("Invalid file.");
            for (int row = 2; row <= sheet.Dimension.Rows; row++)
            {
                var cityName = sheet.Cells[row, 1].Text.Trim();
                if (string.IsNullOrWhiteSpace(cityName)) { skippedRows.Add(row); continue; }
                bool isPopular = sheet.Cells[row, 3].Text == "1" || sheet.Cells[row, 3].Text.ToLower() == "true";
                var prefix = sheet.Cells[row, 4].Text.Trim();
                var existing = existingCities.FirstOrDefault(x => x.CityName.ToLower() == cityName.ToLower());
                if (existing != null)
                {
                    existing.StateName = sheet.Cells[row, 2].Text.Trim();
                    existing.IsPopular = isPopular;
                    existing.PincodePrefix = string.IsNullOrWhiteSpace(prefix) ? existing.PincodePrefix : prefix;
                    updatedCount++;
                }
                else
                {
                    toInsert.Add(new City
                    {
                        CityName = cityName,
                        StateName = sheet.Cells[row, 2].Text.Trim(),
                        IsPopular = isPopular,
                        PincodePrefix = string.IsNullOrWhiteSpace(prefix) ? null : prefix
                    });
                }
            }
            if (toInsert.Any()) await _context.Cities.AddRangeAsync(toInsert);
            if (toInsert.Any() || updatedCount > 0) await _context.SaveChangesAsync();
            var msg = $"{toInsert.Count} inserted, {updatedCount} updated.";
            if (skippedRows.Any()) msg += $" Skipped: {string.Join(",", skippedRows)}";
            return Ok(msg);
        }
    }
}
