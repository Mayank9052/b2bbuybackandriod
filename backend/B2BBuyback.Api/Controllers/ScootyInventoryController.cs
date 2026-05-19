using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using BGaussCRM.API.Data;
using BGaussCRM.API.Models;
using BGaussCRM.API.DTOs;

namespace BGaussCRM.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScootyInventoryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ScootyInventoryController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // =============================
        // GET ALL INVENTORY
        // =============================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.ScootyInventories
                .Include(x => x.Model)
                .Include(x => x.Variant)
                .Include(x => x.Colour)
                .Include(x => x.ScootySpec)
                .Select(x => new
                {
                    x.ScootyId,
                    x.ModelId,
                    ModelName = x.Model.ModelName,
                    x.VariantId,
                    VariantName = x.Variant.VariantName,
                    x.ColourId,
                    ColourName = x.Colour != null ? x.Colour.ColourName : null,
                    x.Price,
                    x.BatterySpecs,
                    x.RangeKm,
                    x.StockAvailable,
                    x.ImageUrl,
                    x.MaxPowerKw,
                    x.BrakeFront,
                    x.BrakeRear,
                    x.BrakingType,
                    x.WheelSize,
                    x.WheelType,
                    x.ChargingTimeHrs,
                    x.StartingType,
                    x.Speedometer,
                    ScootySpec = x.ScootySpec != null ? new
                    {
                        x.ScootySpec.FuelType,
                        x.ScootySpec.ReverseMode,
                        x.ScootySpec.CruiseControl,
                        x.ScootySpec.UsbCharging,
                        x.ScootySpec.RidingModes,
                        x.ScootySpec.WaterWading,
                        x.ScootySpec.GroundClearance,
                        x.ScootySpec.VehicleWeight,
                        x.ScootySpec.BatteryWarranty,
                        x.ScootySpec.MotorWarranty
                    } : null
                })
                .ToListAsync();

            return Ok(data);
        }

        // =============================
        // GET INVENTORY BY ID
        // =============================
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var data = await _context.ScootyInventories
                .Include(x => x.Model)
                .Include(x => x.Variant)
                .Include(x => x.Colour)
                .Include(x => x.ScootySpec)
                .FirstOrDefaultAsync(x => x.ScootyId == id);

            if (data == null)
                return NotFound();

            // Map to DTO
            var dto = new ScootyInventoryDto
            {
                ScootyId = data.ScootyId,
                ModelName = data.Model?.ModelName ?? "",
                VariantName = data.Variant?.VariantName ?? "",
                ColourName = data.Colour?.ColourName ?? "",   // Corrected property
                Price = data.Price,
                StockAvailable = data.StockAvailable,
                ImageUrl = data.ImageUrl,
                ScootySpec = data.ScootySpec == null ? null : new ScootySpecDto
                {
                    FuelType = data.ScootySpec.FuelType,
                    ReverseMode = data.ScootySpec.ReverseMode,
                    CruiseControl = data.ScootySpec.CruiseControl,
                    UsbCharging = data.ScootySpec.UsbCharging,
                    RidingModes = data.ScootySpec.RidingModes,
                    WaterWading = data.ScootySpec.WaterWading,
                    GroundClearance = data.ScootySpec.GroundClearance,
                    VehicleWeight = data.ScootySpec.VehicleWeight,
                    BatteryWarranty = data.ScootySpec.BatteryWarranty,
                    MotorWarranty = data.ScootySpec.MotorWarranty
                }
            };

            return Ok(dto);
        }

        // =============================
        // ADD INVENTORY ITEM
        // =============================
        [HttpPost("add-item")]
        public async Task<IActionResult> AddItem(
            [FromForm] int modelId,
            [FromForm] int variantId,
            [FromForm] int? colourId,
            [FromForm] decimal? price,
            [FromForm] string? batterySpecs,
            [FromForm] int? rangeKm,
            [FromForm] bool stockAvailable,
            [FromForm] IFormFile? image,
            // ScootyInventory extra fields
            [FromForm] decimal? maxPowerKw,
            [FromForm] string? brakeFront,
            [FromForm] string? brakeRear,
            [FromForm] string? brakingType,
            [FromForm] string? wheelSize,
            [FromForm] string? wheelType,
            [FromForm] string? chargingTimeHrs,
            [FromForm] string? startingType,
            [FromForm] string? speedometer,
            // ScootySpec fields
            [FromForm] string? fuelType,
            [FromForm] bool reverseMode,
            [FromForm] bool cruiseControl,
            [FromForm] bool usbCharging,
            [FromForm] string? ridingModes,
            [FromForm] string? waterWading,
            [FromForm] int? groundClearance,
            [FromForm] int? vehicleWeight,
            [FromForm] string? batteryWarranty,
            [FromForm] string? motorWarranty
        )
        {
            try
            {
                string? imagePath = null;

                if (image != null && image.Length > 0)
                {
                    var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    string folderPath = Path.Combine(rootPath, "ScootyInventoryImage");
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    string fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                    string filePath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(stream);
                    }

                    imagePath = "/ScootyInventoryImage/" + fileName;
                }

                // Validate foreign keys
                if (!await _context.VehicleModels.AnyAsync(x => x.Id == modelId))
                    return BadRequest($"Model ID {modelId} does not exist.");
                if (!await _context.VehicleVariants.AnyAsync(x => x.Id == variantId))
                    return BadRequest($"Variant ID {variantId} does not exist.");
                if (colourId.HasValue && !await _context.VehicleColours.AnyAsync(x => x.Id == colourId.Value))
                    return BadRequest($"Colour ID {colourId} does not exist.");

                // Duplicate check
                if (await _context.ScootyInventories.AnyAsync(x => x.ModelId == modelId && x.VariantId == variantId && x.ColourId == colourId))
                    return BadRequest("This item already exists. Duplicate entries are not allowed.");

                // Create inventory object
                var inventory = new ScootyInventory
                {
                    ModelId = modelId,
                    VariantId = variantId,
                    ColourId = colourId,
                    Price = price,
                    BatterySpecs = batterySpecs,
                    RangeKm = rangeKm,
                    StockAvailable = stockAvailable,
                    ImageUrl = imagePath,
                    MaxPowerKw = maxPowerKw,
                    BrakeFront = brakeFront,
                    BrakeRear = brakeRear,
                    BrakingType = brakingType,
                    WheelSize = wheelSize,
                    WheelType = wheelType,
                    ChargingTimeHrs = chargingTimeHrs,
                    StartingType = startingType,
                    Speedometer = speedometer
                };

                // Add ScootySpec
                if (!string.IsNullOrWhiteSpace(fuelType) || reverseMode || cruiseControl || usbCharging || !string.IsNullOrWhiteSpace(ridingModes) ||
                    !string.IsNullOrWhiteSpace(waterWading) || groundClearance.HasValue || vehicleWeight.HasValue || !string.IsNullOrWhiteSpace(batteryWarranty) || !string.IsNullOrWhiteSpace(motorWarranty))
                {
                    inventory.ScootySpec = new ScootySpec
                    {
                        FuelType = fuelType,
                        ReverseMode = reverseMode,
                        CruiseControl = cruiseControl,
                        UsbCharging = usbCharging,
                        RidingModes = ridingModes,
                        WaterWading = waterWading,
                        GroundClearance = groundClearance,
                        VehicleWeight = vehicleWeight,
                        BatteryWarranty = batteryWarranty,
                        MotorWarranty = motorWarranty
                    };
                }

                _context.ScootyInventories.Add(inventory);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Scooty item added successfully", data = inventory });
            }
            catch (DbUpdateException)
            {
                return BadRequest("Duplicate entry not allowed for same Model, Variant, and Colour.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        // =============================
        // UPDATE INVENTORY ITEM
        // =============================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            [FromForm] int modelId,
            [FromForm] int variantId,
            [FromForm] int? colourId,
            [FromForm] decimal? price,
            [FromForm] string? batterySpecs,
            [FromForm] int? rangeKm,
            [FromForm] bool stockAvailable,
            [FromForm] IFormFile? image,
            // ScootyInventory extra fields
            [FromForm] decimal? maxPowerKw,
            [FromForm] string? brakeFront,
            [FromForm] string? brakeRear,
            [FromForm] string? brakingType,
            [FromForm] string? wheelSize,
            [FromForm] string? wheelType,
            [FromForm] string? chargingTimeHrs,
            [FromForm] string? startingType,
            [FromForm] string? speedometer,
            // ScootySpec fields
            [FromForm] string? fuelType,
            [FromForm] bool reverseMode,
            [FromForm] bool cruiseControl,
            [FromForm] bool usbCharging,
            [FromForm] string? ridingModes,
            [FromForm] string? waterWading,
            [FromForm] int? groundClearance,
            [FromForm] int? vehicleWeight,
            [FromForm] string? batteryWarranty,
            [FromForm] string? motorWarranty
        )
        {
            var existing = await _context.ScootyInventories
                .Include(x => x.ScootySpec)
                .FirstOrDefaultAsync(x => x.ScootyId == id);

            if (existing == null)
                return NotFound();

            existing.ModelId = modelId;
            existing.VariantId = variantId;
            existing.ColourId = colourId;
            existing.Price = price;
            existing.BatterySpecs = batterySpecs;
            existing.RangeKm = rangeKm;
            existing.StockAvailable = stockAvailable;
            existing.MaxPowerKw = maxPowerKw;
            existing.BrakeFront = brakeFront;
            existing.BrakeRear = brakeRear;
            existing.BrakingType = brakingType;
            existing.WheelSize = wheelSize;
            existing.WheelType = wheelType;
            existing.ChargingTimeHrs = chargingTimeHrs;
            existing.StartingType = startingType;
            existing.Speedometer = speedometer;

            if (image != null && image.Length > 0)
            {
                var imagePath = SaveImage(image);
                existing.ImageUrl = imagePath;
            }

            // Update or create ScootySpec
            if (existing.ScootySpec == null)
                existing.ScootySpec = new ScootySpec();

            existing.ScootySpec.FuelType = fuelType;
            existing.ScootySpec.ReverseMode = reverseMode;
            existing.ScootySpec.CruiseControl = cruiseControl;
            existing.ScootySpec.UsbCharging = usbCharging;
            existing.ScootySpec.RidingModes = ridingModes;
            existing.ScootySpec.WaterWading = waterWading;
            existing.ScootySpec.GroundClearance = groundClearance;
            existing.ScootySpec.VehicleWeight = vehicleWeight;
            existing.ScootySpec.BatteryWarranty = batteryWarranty;
            existing.ScootySpec.MotorWarranty = motorWarranty;

            await _context.SaveChangesAsync();
            return Ok(existing);
        }

        // =============================
        // DELETE INVENTORY ITEM
        // =============================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var data = await _context.ScootyInventories.FindAsync(id);
            if (data == null) return NotFound();

            _context.ScootyInventories.Remove(data);
            await _context.SaveChangesAsync();

            return Ok("Deleted successfully");
        }

        // =============================
        // SAVE IMAGE
        // =============================
        private string SaveImage(IFormFile image)
        {
            string folderPath = Path.Combine(_environment.WebRootPath, "ScootyInventoryImage");
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
            string filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                image.CopyTo(stream);
            }

            return "/ScootyInventoryImage/" + fileName;
        }

         // =============================
        // GET MODELS (DROPDOWN)
        // =============================
        [HttpGet("models")]
        public async Task<IActionResult> GetModels()
        {
            var models = await _context.VehicleModels
                .Select(x => new
                {
                    x.Id,
                    x.ModelName
                })
                .ToListAsync();

            return Ok(models);
        }

        // =============================
        // GET VARIANTS BY MODEL
        // =============================
        [HttpGet("variants/{modelId}")]
        public async Task<IActionResult> GetVariants(int modelId)
        {
            var variants = await _context.VehicleVariants
                .Where(x => x.ModelId == modelId)
                .Select(x => new
                {
                    x.Id,
                    x.VariantName
                })
                .ToListAsync();

            return Ok(variants);
        }

        // =============================
        // GET COLOURS BY MODEL + VARIANT
        // =============================
        [HttpGet("colours")]
        public async Task<IActionResult> GetColours(int modelId, int variantId)
        {
            var colours = await _context.VehicleColours
                .Where(x => x.ModelId == modelId && x.VariantId == variantId)
                .Select(x => new
                {
                    x.Id,
                    x.ColourName
                })
                .ToListAsync();

            return Ok(colours);
        }

        // =============================
        // GET UNIQUE MODEL LIST (ONLY 1 IMAGE PER MODEL)
        // =============================
        [HttpGet("models-list")]
        public async Task<IActionResult> GetUniqueModels(
            [FromQuery] string? pincode = null,
            [FromQuery] int?    cityId  = null)
        {
            // ── Resolve area IDs from pincode or cityId ──
            List<int>? areaIds = null;

            if (!string.IsNullOrWhiteSpace(pincode))
            {
                var cleanPin = pincode.Trim();

                // Try exact match
                var area = await _context.CityAreas
                    .FirstOrDefaultAsync(a => a.Pincode == cleanPin);

                // Prefix fallback
                if (area == null && cleanPin.Length >= 3)
                    area = await _context.CityAreas
                        .FirstOrDefaultAsync(a => a.Pincode.StartsWith(cleanPin.Substring(0, 3)));

                if (area != null)
                    areaIds = new List<int> { area.Id };
            }
            else if (cityId.HasValue)
            {
                areaIds = await _context.CityAreas
                    .Where(a => a.CityId == cityId.Value)
                    .Select(a => a.Id)
                    .ToListAsync();
            }

            // ── With location context: use area-specific stock ──
            if (areaIds != null && areaIds.Count > 0)
            {
                var areaStockData = await _context.AreaScootyStocks
                    .Include(s => s.Scooty).ThenInclude(x => x.Model)
                    .Include(s => s.Scooty).ThenInclude(x => x.Variant)
                    .Where(s => areaIds.Contains(s.CityAreaId)
                            && s.StockQuantity > 0
                            && s.Scooty.ImageUrl != null)
                    .ToListAsync();

                // One entry per model (highest stock)
                var grouped = areaStockData
                    .GroupBy(s => s.Scooty.ModelId)
                    .Select(g => g.OrderByDescending(s => s.StockQuantity).First())
                    .Select(s => new
                    {
                        ScootyId       = s.ScootyId,
                        ModelId        = s.Scooty.ModelId,
                        ModelName      = s.Scooty.Model.ModelName,
                        VariantName    = s.Scooty.Variant.VariantName,
                        ImageUrl       = s.Scooty.ImageUrl,
                        StockAvailable = s.StockAvailable,
                        StockQuantity  = s.StockQuantity,
                        Price          = s.Scooty.Price,
                        RangeKm        = s.Scooty.RangeKm
                    })
                    .ToList();

                return Ok(grouped);
            }

            // ── No location: global list ──
            var data = await _context.ScootyInventories
                .Include(x => x.Model)
                .Include(x => x.Variant)
                .Where(x => x.ImageUrl != null && x.StockAvailable)
                .GroupBy(x => x.ModelId)
                .Select(g => g
                    .OrderByDescending(x => x.ScootyId)
                    .Select(x => new
                    {
                        x.ScootyId,
                        ModelId        = x.ModelId,
                        ModelName      = x.Model.ModelName,
                        VariantName    = x.Variant.VariantName,
                        x.ImageUrl,
                        x.StockAvailable,
                        x.StockQuantity,
                        x.Price,
                        x.RangeKm
                    })
                    .FirstOrDefault()
                )
                .ToListAsync();

            return Ok(data);
        }

        // =============================
        // GET DETAILS FOR CLICKED SCOOTY ONLY
        // =============================
        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetScootyDetails(
            int id,
            [FromQuery] string? pincode = null,
            [FromQuery] int?    cityId  = null)
        {
            var data = await _context.ScootyInventories
                .Include(x => x.Model)
                .Include(x => x.Variant)
                .Include(x => x.Colour)
                .Where(x => x.ScootyId == id)
                .Select(x => new
                {
                    x.ImageUrl,
                    x.ScootyId,
                    ModelName   = x.Model.ModelName,
                    VariantName = x.Variant.VariantName,
                    ColourName  = x.Colour != null ? x.Colour.ColourName : null,
                    x.Price,
                    x.BatterySpecs,
                    x.RangeKm,
                    x.StockAvailable,
                    x.StockQuantity,
                    // New spec fields
                    x.MaxPowerKw,
                    x.BrakeFront,
                    x.BrakeRear,
                    x.BrakingType,
                    x.WheelSize,
                    x.WheelType,
                    x.ChargingTimeHrs,
                    x.StartingType,
                    x.Speedometer
                })
                .FirstOrDefaultAsync();

            if (data == null) return NotFound();

            // ── Resolve area-specific stock if location provided ──
            object? areaStockInfo = null;

            if (!string.IsNullOrWhiteSpace(pincode) || cityId.HasValue)
            {
                List<int> areaIds = new();

                if (!string.IsNullOrWhiteSpace(pincode))
                {
                    var cleanPin = pincode.Trim();
                    var area = await _context.CityAreas
                        .FirstOrDefaultAsync(a => a.Pincode == cleanPin);
                    if (area == null && cleanPin.Length >= 3)
                        area = await _context.CityAreas
                            .FirstOrDefaultAsync(a => a.Pincode.StartsWith(cleanPin.Substring(0, 3)));
                    if (area != null) areaIds.Add(area.Id);
                }
                else if (cityId.HasValue)
                {
                    areaIds = await _context.CityAreas
                        .Where(a => a.CityId == cityId.Value)
                        .Select(a => a.Id)
                        .ToListAsync();
                }

                if (areaIds.Count > 0)
                {
                    var areaStock = await _context.AreaScootyStocks
                        .Include(s => s.CityArea).ThenInclude(a => a.City)
                        .Where(s => s.ScootyId == id && areaIds.Contains(s.CityAreaId))
                        .Select(s => new
                        {
                            s.CityAreaId,
                            AreaName      = s.CityArea.AreaName,
                            Pincode       = s.CityArea.Pincode,
                            CityName      = s.CityArea.City.CityName,
                            s.StockQuantity,
                            s.StockAvailable
                        })
                        .FirstOrDefaultAsync();

                    areaStockInfo = areaStock;
                }
            }

            // Return merged result
            return Ok(new
            {
                data.ImageUrl,
                data.ScootyId,
                data.ModelName,
                data.VariantName,
                data.ColourName,
                data.Price,
                data.BatterySpecs,
                data.RangeKm,
                // If area-specific stock exists, use it; else use global
                StockAvailable = areaStockInfo != null
                    ? ((dynamic)areaStockInfo).StockAvailable
                    : data.StockAvailable,
                StockQuantity  = areaStockInfo != null
                    ? (int)((dynamic)areaStockInfo).StockQuantity
                    : data.StockQuantity,
                data.MaxPowerKw,
                data.BrakeFront,
                data.BrakeRear,
                data.BrakingType,
                data.WheelSize,
                data.WheelType,
                data.ChargingTimeHrs,
                data.StartingType,
                data.Speedometer,
                AreaStock = areaStockInfo  // null if no location context
            });
        }

        // =============================
        // SHARE ACTIVE SCOOTY DETAILS
        // =============================
        [HttpGet("share/{id}")]
        public async Task<IActionResult> ShareScooty(int id)
        {
            var data = await _context.ScootyInventories
                .Include(x => x.Model)
                .Include(x => x.Variant)
                .Include(x => x.Colour)
                .Where(x => x.ScootyId == id && x.StockAvailable)
                .Select(x => new
                {
                    Title = x.Model.ModelName + " " + x.Variant.VariantName,
                    Model = x.Model.ModelName,
                    Variant = x.Variant.VariantName,
                    Colour = x.Colour != null ? x.Colour.ColourName : null,
                    Price = x.Price,
                    Range = x.RangeKm,
                    Battery = x.BatterySpecs,
                    x.ImageUrl,
                    ShareText =
                        "Scooty Details:\n" +
                        "Model: " + x.Model.ModelName + "\n" +
                        "Variant: " + x.Variant.VariantName + "\n" +
                        "Price: ₹" + x.Price + "\n" +
                        "Range: " + x.RangeKm + " km\n" +
                        "Battery: " + x.BatterySpecs
                })
                .FirstOrDefaultAsync();

            if (data == null)
                return NotFound("Scooty not found or not active");

            return Ok(data);
        }

        // =============================
        // DOWNLOAD EXCEL TEMPLATE
        // =============================
        [HttpGet("download-template")]
        public IActionResult DownloadTemplate()
        {
            using var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("ScootyInventory");

            // ScootyInventory Columns
            sheet.Cells[1, 1].Value = "ModelId";
            sheet.Cells[1, 2].Value = "VariantId";
            sheet.Cells[1, 3].Value = "ColourId";
            sheet.Cells[1, 4].Value = "Price";
            sheet.Cells[1, 5].Value = "BatterySpecs";
            sheet.Cells[1, 6].Value = "RangeKm";
            sheet.Cells[1, 7].Value = "StockAvailable";
            sheet.Cells[1, 8].Value = "ImageUrl";
            sheet.Cells[1, 9].Value = "MaxPowerKw";
            sheet.Cells[1, 10].Value = "BrakeFront";
            sheet.Cells[1, 11].Value = "BrakeRear";
            sheet.Cells[1, 12].Value = "BrakingType";
            sheet.Cells[1, 13].Value = "WheelSize";
            sheet.Cells[1, 14].Value = "WheelType";
            sheet.Cells[1, 15].Value = "ChargingTimeHrs";
            sheet.Cells[1, 16].Value = "StartingType";
            sheet.Cells[1, 17].Value = "Speedometer";

            // ScootySpec Columns
            sheet.Cells[1, 18].Value = "FuelType";
            sheet.Cells[1, 19].Value = "ReverseMode";
            sheet.Cells[1, 20].Value = "CruiseControl";
            sheet.Cells[1, 21].Value = "UsbCharging";
            sheet.Cells[1, 22].Value = "RidingModes";
            sheet.Cells[1, 23].Value = "WaterWading";
            sheet.Cells[1, 24].Value = "GroundClearance";
            sheet.Cells[1, 25].Value = "VehicleWeight";
            sheet.Cells[1, 26].Value = "BatteryWarranty";
            sheet.Cells[1, 27].Value = "MotorWarranty";

            var stream = new MemoryStream(package.GetAsByteArray());

            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "ScootyInventoryTemplate.xlsx");
        }

        // =============================
        // IMPORT EXCEL
        // =============================
        [HttpPost("import")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var inventoryToInsert = new List<ScootyInventory>();
            int updatedCount = 0;
            var skippedRows = new List<int>();

            // Get existing inventory including ScootySpec
            var existingInventory = await _context.ScootyInventories
                .Include(x => x.ScootySpec)
                .ToListAsync();

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var package = new ExcelPackage(stream);
            var sheet = package.Workbook.Worksheets.FirstOrDefault();
            if (sheet == null) return BadRequest("Invalid Excel file.");

            int rowCount = sheet.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                // Initialize variables to remove CS0165
                int modelId = 0;
                int variantId = 0;

                // Try parsing modelId and variantId
                bool parseSuccess = int.TryParse(sheet.Cells[row, 1].Text, out modelId)
                                && int.TryParse(sheet.Cells[row, 2].Text, out variantId);

                if (!parseSuccess)
                {
                    skippedRows.Add(row);
                    continue;
                }

                int? colourId = int.TryParse(sheet.Cells[row, 3].Text, out int cId) ? cId : null;
                decimal? price = decimal.TryParse(sheet.Cells[row, 4].Text, out decimal p) ? p : null;
                string? batterySpecs = sheet.Cells[row, 5].Text;
                int? rangeKm = int.TryParse(sheet.Cells[row, 6].Text, out int r) ? r : null;
                bool stockAvailable = sheet.Cells[row, 7].Text == "1" || sheet.Cells[row, 7].Text.ToLower() == "true";
                string? imageUrl = string.IsNullOrWhiteSpace(sheet.Cells[row, 8].Text) ? null : sheet.Cells[row, 8].Text.Trim();
                decimal? maxPowerKw = decimal.TryParse(sheet.Cells[row, 9].Text, out decimal mp) ? mp : null;
                string? brakeFront = sheet.Cells[row, 10].Text;
                string? brakeRear = sheet.Cells[row, 11].Text;
                string? brakingType = sheet.Cells[row, 12].Text;
                string? wheelSize = sheet.Cells[row, 13].Text;
                string? wheelType = sheet.Cells[row, 14].Text;
                string? chargingTimeHrs = sheet.Cells[row, 15].Text;
                string? startingType = sheet.Cells[row, 16].Text;
                string? speedometer = sheet.Cells[row, 17].Text;

                // ScootySpec Fields
                string? fuelType = sheet.Cells[row, 18].Text;
                bool reverseMode = sheet.Cells[row, 19].Text == "1" || sheet.Cells[row, 19].Text.ToLower() == "true";
                bool cruiseControl = sheet.Cells[row, 20].Text == "1" || sheet.Cells[row, 20].Text.ToLower() == "true";
                bool usbCharging = sheet.Cells[row, 21].Text == "1" || sheet.Cells[row, 21].Text.ToLower() == "true";
                string? ridingModes = sheet.Cells[row, 22].Text;
                string? waterWading = sheet.Cells[row, 23].Text;
                int? groundClearance = int.TryParse(sheet.Cells[row, 24].Text, out int gc) ? gc : null;
                int? vehicleWeight = int.TryParse(sheet.Cells[row, 25].Text, out int vw) ? vw : null;
                string? batteryWarranty = sheet.Cells[row, 26].Text;
                string? motorWarranty = sheet.Cells[row, 27].Text;

                // Check if inventory exists
                var existing = existingInventory.FirstOrDefault(x =>
                    x.ModelId == modelId &&
                    x.VariantId == variantId &&
                    x.ColourId == colourId
                );

                if (existing != null)
                {
                    // Update existing ScootyInventory
                    existing.Price = price;
                    existing.BatterySpecs = batterySpecs;
                    existing.RangeKm = rangeKm;
                    existing.StockAvailable = stockAvailable;
                    existing.ImageUrl = imageUrl;
                    existing.MaxPowerKw = maxPowerKw;
                    existing.BrakeFront = brakeFront;
                    existing.BrakeRear = brakeRear;
                    existing.BrakingType = brakingType;
                    existing.WheelSize = wheelSize;
                    existing.WheelType = wheelType;
                    existing.ChargingTimeHrs = chargingTimeHrs;
                    existing.StartingType = startingType;
                    existing.Speedometer = speedometer;

                    // Update or create ScootySpec
                    if (existing.ScootySpec == null)
                        existing.ScootySpec = new ScootySpec();

                    existing.ScootySpec.FuelType = fuelType;
                    existing.ScootySpec.ReverseMode = reverseMode;
                    existing.ScootySpec.CruiseControl = cruiseControl;
                    existing.ScootySpec.UsbCharging = usbCharging;
                    existing.ScootySpec.RidingModes = ridingModes;
                    existing.ScootySpec.WaterWading = waterWading;
                    existing.ScootySpec.GroundClearance = groundClearance;
                    existing.ScootySpec.VehicleWeight = vehicleWeight;
                    existing.ScootySpec.BatteryWarranty = batteryWarranty;
                    existing.ScootySpec.MotorWarranty = motorWarranty;

                    updatedCount++;
                    continue;
                }

                // Add new ScootyInventory with ScootySpec
                var inventory = new ScootyInventory
                {
                    ModelId = modelId,
                    VariantId = variantId,
                    ColourId = colourId,
                    Price = price,
                    BatterySpecs = batterySpecs,
                    RangeKm = rangeKm,
                    StockAvailable = stockAvailable,
                    ImageUrl = imageUrl,
                    MaxPowerKw = maxPowerKw,
                    BrakeFront = brakeFront,
                    BrakeRear = brakeRear,
                    BrakingType = brakingType,
                    WheelSize = wheelSize,
                    WheelType = wheelType,
                    ChargingTimeHrs = chargingTimeHrs,
                    StartingType = startingType,
                    Speedometer = speedometer,
                    ScootySpec = new ScootySpec
                    {
                        FuelType = fuelType,
                        ReverseMode = reverseMode,
                        CruiseControl = cruiseControl,
                        UsbCharging = usbCharging,
                        RidingModes = ridingModes,
                        WaterWading = waterWading,
                        GroundClearance = groundClearance,
                        VehicleWeight = vehicleWeight,
                        BatteryWarranty = batteryWarranty,
                        MotorWarranty = motorWarranty
                    }
                };

                inventoryToInsert.Add(inventory);
            }

            // Save all
            if (inventoryToInsert.Any())
                await _context.ScootyInventories.AddRangeAsync(inventoryToInsert);

            if (inventoryToInsert.Any() || updatedCount > 0)
                await _context.SaveChangesAsync();

            var message = $"{inventoryToInsert.Count} inserted, {updatedCount} updated.";
            if (skippedRows.Any())
                message += $" Skipped rows: {string.Join(",", skippedRows)}";

            return Ok(message);
        }
    }
}