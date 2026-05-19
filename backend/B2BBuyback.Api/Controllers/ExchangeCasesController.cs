// Controllers/ExchangeCasesController.cs
// CHANGES:
//   • GetCurrentUserAsync()  → returns full User object (FullName + Email)
//   • ResolveDealerAsync()   → returns full User object from DealerId
//   • Submit()               → passes User to SendCaseSubmissionEmailsAsync
//   • AdminAction()          → passes User to SendAdminActionEmailAsync
//   • GetMyCases()           → dealer dashboard endpoint (unchanged)

using B2BBuyback.Api.Data;
using B2BBuyback.Api.DTOs;
using B2BBuyback.Api.Interfaces;
using B2BBuyback.Api.Models;
using B2BBuyback.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace B2BBuyback.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExchangeCasesController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<ExchangeCasesController> _logger;
        private readonly IExchangeEmailService _email;

        private const string IMG_FOLDER = "ExchangeImages";

        private static readonly Dictionary<string, string[]> InspectionParams = new()
        {
            ["Battery"]     = new[] { "Health", "Charge Capacity", "Physical Damage" },
            ["Body"]        = new[] { "Dents", "Scratches", "Paint Condition" },
            ["Tyres"]       = new[] { "Tread Depth", "Condition", "Age" },
            ["Electricals"] = new[] { "Lights", "Horn", "Indicators", "Charging Port" },
            ["Misc"]        = new[] { "Documentation", "Accessories", "Service History" },
        };

        private static readonly Dictionary<string, decimal> CategoryWeights = new()
        {
            ["Battery"]     = 0.35m,
            ["Body"]        = 0.25m,
            ["Tyres"]       = 0.15m,
            ["Electricals"] = 0.15m,
            ["Misc"]        = 0.10m,
        };

        public ExchangeCasesController(
            AppDbContext db,
            IWebHostEnvironment env,
            ILogger<ExchangeCasesController> logger,
            IExchangeEmailService email)
        {
            _db     = db;
            _env    = env;
            _logger = logger;
            _email  = email;
        }

        // ── Resolve the current logged-in user as a full User object ──────────
        // Priority: UserId JWT claim → DB lookup by UserId
        // Fallback:  email claim → DB lookup by email/username
        // Last resort: returns a minimal User so emails still send
        private async Task<User> GetCurrentUserAsync()
        {
            // 1. Try UserId claim (set in AuthController.GenerateJwtToken)
            var userIdStr = User.FindFirst("UserId")?.Value;
            if (int.TryParse(userIdStr, out var userId))
            {
                var dbUser = await _db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == userId);
                if (dbUser != null) return dbUser;
            }

            // 2. Try email / name claim
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value
                ?? User.Identity?.Name
                ?? User.FindFirst(ClaimTypes.Name)?.Value
                ?? User.FindFirst("sub")?.Value;

            if (!string.IsNullOrWhiteSpace(emailClaim))
            {
                var dbUser = await _db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == emailClaim || u.Username == emailClaim);
                if (dbUser != null) return dbUser;
            }

            // 3. Last resort — minimal object so emails still send
            _logger.LogWarning("GetCurrentUserAsync: could not resolve user from JWT claims");
            return new User
            {
                FullName = "BGauss Dealer",
                Email    = "priyanka.nikam@bgauss.com",
                Username = "fallback"
            };
        }

        // ── Resolve dealer User from DealerId stored on the case ─────────────
        // DealerId was saved as the dealer's email at case creation time.
        // We cross-reference the Users table to get FullName and canonical Email.
        private async Task<User> ResolveDealerAsync(string dealerId)
        {
            if (!string.IsNullOrWhiteSpace(dealerId))
            {
                var user = await _db.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == dealerId || u.Username == dealerId);
                if (user != null)
                {
                    _logger.LogInformation(
                        "ResolveDealerAsync: {DealerId} → {FullName} <{Email}>",
                        dealerId, user.FullName, user.Email);
                    return user;
                }
            }

            // Fallback: construct minimal User from DealerId (which is an email)
            _logger.LogWarning("ResolveDealerAsync: could not find User for DealerId={DealerId}", dealerId);
            return new User
            {
                FullName = dealerId?.Split('@')[0] ?? "BGauss Dealer",
                Email    = dealerId?.Contains("@") == true ? dealerId : "priyanka.nikam@bgauss.com",
                Username = dealerId ?? "unknown"
            };
        }

        // CurrentUser string — used only for authorization comparisons (DealerId on case)
        private string CurrentUser
        {
            get
            {
                var user = User.FindFirst(ClaimTypes.Email)?.Value
                    ?? User.Identity?.Name
                    ?? User.FindFirst(ClaimTypes.Name)?.Value
                    ?? User.FindFirst("sub")?.Value;

                return string.IsNullOrWhiteSpace(user) ? "priyanka.nikam@bgauss.com" : user;
            }
        }

        private bool IsAdmin => User.IsInRole("admin");

        // ── WebRoot resolver ──────────────────────────────────────────────────
        private string GetWebRoot()
        {
            if (!string.IsNullOrWhiteSpace(_env.WebRootPath) &&
                Directory.Exists(_env.WebRootPath) && CanWrite(_env.WebRootPath))
                return _env.WebRootPath;

            var beside = Path.Combine(_env.ContentRootPath, "wwwroot");
            EnsureDir(beside);
            if (CanWrite(beside)) return beside;

            var tmp = Path.Combine(Path.GetTempPath(), "bgauss-uploads");
            EnsureDir(tmp);
            _logger.LogWarning("Using temp upload path: {Path}.", tmp);
            return tmp;
        }

        private static bool CanWrite(string path)
        {
            try
            {
                var p = Path.Combine(path, $".probe_{Guid.NewGuid():N}");
                System.IO.File.WriteAllText(p, "ok");
                System.IO.File.Delete(p);
                return true;
            }
            catch { return false; }
        }

        private void EnsureDir(string path)
        {
            if (!Directory.Exists(path))
                try { Directory.CreateDirectory(path); }
                catch (Exception ex) { _logger.LogWarning(ex, "Could not create directory: {Path}", path); }
        }

        // ── GET /api/ExchangeCases ────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            var q = _db.ExchangeCases.AsQueryable();
            if (!IsAdmin) q = q.Where(c => c.DealerId == CurrentUser);
            if (!string.IsNullOrEmpty(status)) q = q.Where(c => c.Status == status);

            var list = await q.OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id, c.CaseNumber, c.CustomerName, c.MobileNumber, c.City,
                    c.VehicleModel, c.RegistrationNo, c.YearOfPurchase, c.KmDriven,
                    c.Grade, c.TotalScore, c.RecommendedPrice, c.MinPrice, c.MaxPrice,
                    c.Status, c.DealerId, c.ApprovedPrice, c.AdminNote,
                    c.CreatedAt, c.SubmittedAt, c.AdminActionAt,
                    ImageCount = c.ExchangeCaseImages.Count,
                    ScoreCount = c.ExchangeInspectionScores.Count,
                })
                .ToListAsync();

            return Ok(list);
        }

        // ── GET /api/ExchangeCases/my-cases ───────────────────────────────────
        [HttpGet("my-cases")]
        public async Task<IActionResult> GetMyCases([FromQuery] int limit = 10)
        {
            var dealerId = CurrentUser;

            var cases = await _db.ExchangeCases
                .Where(c => c.DealerId == dealerId)
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .Select(c => new
                {
                    c.Id,
                    c.CaseNumber,
                    c.CustomerName,
                    c.VehicleModel,
                    c.RegistrationNo,
                    c.Status,
                    c.Grade,
                    c.TotalScore,
                    c.RecommendedPrice,
                    c.MinPrice,
                    c.MaxPrice,
                    c.ApprovedPrice,
                    c.AdminNote,
                    c.CreatedAt,
                    c.SubmittedAt,
                    c.AdminActionAt,
                })
                .ToListAsync();

            return Ok(cases);
        }

        // ── GET /api/ExchangeCases/{id} ───────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var c = await _db.ExchangeCases
                .Include(x => x.ExchangeCaseImages)
                .Include(x => x.ExchangeAdminActions)
                .Include(x => x.ExchangeInspectionScores)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c == null) return NotFound();
            if (!IsAdmin && c.DealerId != CurrentUser) return Forbid();
            return Ok(c);
        }

        // ── GET /api/ExchangeCases/inspection-params ──────────────────────────
        [HttpGet("inspection-params")]
        public IActionResult GetInspectionParams()
            => Ok(InspectionParams.Select(kv => new { category = kv.Key, parameters = kv.Value }));

        // ── POST /api/ExchangeCases/start ─────────────────────────────────────
        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] StartCaseDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var caseNum = $"EX-{DateTime.UtcNow.Year}-{(await _db.ExchangeCases.CountAsync() + 1):D5}";

            // Resolve dealer email for DealerId storage — we store email as the identity key
            var currentUser  = await GetCurrentUserAsync();
            var dealerEmail  = currentUser.Email;

            var exchangeCase = new ExchangeCase
            {
                CaseNumber     = caseNum,
                CustomerName   = dto.CustomerName,
                MobileNumber   = dto.MobileNumber,
                City           = dto.City,
                VehicleModel   = dto.VehicleModel,
                VehicleVariant = dto.VehicleVariant,
                RegistrationNo = dto.RegistrationNo,
                YearOfPurchase = dto.YearOfPurchase,
                KmDriven       = dto.KmDriven,
                DealerId       = dealerEmail,   // stored as email — used to resolve User later
                Status         = "Draft",
                CreatedAt      = DateTime.UtcNow,
                UpdatedAt      = DateTime.UtcNow
            };

            _db.ExchangeCases.Add(exchangeCase);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                id         = exchangeCase.Id,
                caseNumber = caseNum,
                dealer     = dealerEmail,
                dealerName = currentUser.FullName
            });
        }

        // ── POST /api/ExchangeCases/{id}/scores ───────────────────────────────
        [HttpPost("{id}/scores")]
        public async Task<IActionResult> SaveScores(int id, [FromBody] List<ScoreDto> scores)
        {
            var c = await _db.ExchangeCases.Include(x => x.ExchangeInspectionScores)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound();
            if (c.DealerId != CurrentUser && !IsAdmin) return Forbid();
            if (c.Status != "Draft") return BadRequest("Case is not in Draft status.");

            _db.ExchangeInspectionScores.RemoveRange(c.ExchangeInspectionScores);
            await _db.SaveChangesAsync();

            foreach (var s in scores)
                _db.ExchangeInspectionScores.Add(new ExchangeInspectionScore
                    { CaseId = id, Category = s.Category, Parameter = s.Parameter, Score = s.Score });

            var (totalScore, grade) = ComputeScore(scores);
            c.TotalScore = totalScore;
            c.Grade      = grade;
            c.UpdatedAt  = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { totalScore, grade });
        }

        // ── POST /api/ExchangeCases/{id}/images ───────────────────────────────
        [HttpPost("{id}/images")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage(int id, [FromForm] string imageType, IFormFile image)
        {
            var normalize = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Front View"]    = "Front",
                ["Rear View"]     = "Rear",
                ["Left View"]     = "Left",
                ["Right View"]    = "Right",
                ["Odometer View"] = "Odometer",
                ["Battery View"]  = "Battery"
            };

            if (normalize.ContainsKey(imageType))
                imageType = normalize[imageType];

            var validTypes = new[] { "Front", "Rear", "Left", "Right", "Odometer", "Battery" };
            if (!validTypes.Contains(imageType))
                return BadRequest(new { error = $"Invalid imageType '{imageType}'." });

            var c = await _db.ExchangeCases.Include(x => x.ExchangeCaseImages)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (c == null) return NotFound(new { error = $"Case {id} not found." });
            if (image == null || image.Length == 0) return BadRequest(new { error = "No image provided." });

            var ext = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png" }.Contains(ext))
                return BadRequest(new { error = $"File type '{ext}' not allowed. Use JPG or PNG." });

            var webRoot = GetWebRoot();
            var folder  = Path.Combine(webRoot, IMG_FOLDER, id.ToString());
            try { Directory.CreateDirectory(folder); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cannot create upload directory: {Folder}", folder);
                return StatusCode(500, new { error = "Server cannot create upload directory.", detail = ex.Message });
            }

            var finalName = $"{imageType}{ext}";
            var tempPath  = Path.Combine(folder, $"{imageType}_{Guid.NewGuid():N}{ext}");
            var finalPath = Path.Combine(folder, finalName);

            try
            {
                await using (var fs = System.IO.File.Create(tempPath)) await image.CopyToAsync(fs);
                if (System.IO.File.Exists(finalPath)) System.IO.File.Delete(finalPath);
                System.IO.File.Move(tempPath, finalPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write image: {Path}", finalPath);
                if (System.IO.File.Exists(tempPath)) try { System.IO.File.Delete(tempPath); } catch { }
                return StatusCode(500, new { error = "Failed to save image.", detail = ex.Message });
            }

            var relPath  = $"/{IMG_FOLDER}/{id}/{finalName}";
            var existing = c.ExchangeCaseImages.FirstOrDefault(i => i.ImageType == imageType);
            if (existing != null) existing.ImagePath = relPath;
            else _db.ExchangeCaseImages.Add(new ExchangeCaseImage
                { CaseId = id, ImageType = imageType, ImagePath = relPath });

            c.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { imageType, path = relPath });
        }

        // ── POST /api/ExchangeCases/{id}/generate-price ───────────────────────
        [HttpPost("{id}/generate-price")]
        public async Task<IActionResult> GeneratePrice(int id)
        {
            var c = await _db.ExchangeCases
                .Include(x => x.ExchangeCaseImages)
                .Include(x => x.ExchangeInspectionScores)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c == null) return NotFound();
            if (c.DealerId != CurrentUser && !IsAdmin) return Forbid();

            var required = new[] { "Front", "Rear", "Left", "Right", "Odometer", "Battery" };
            var uploaded = c.ExchangeCaseImages.Select(i => i.ImageType).ToHashSet();
            var missing  = required.Except(uploaded).ToList();
            if (missing.Any()) return BadRequest(new { error = "ImagesMissing", missing });
            if (!c.ExchangeInspectionScores.Any()) return BadRequest(new { error = "ScoresMissing" });

            var modelUpper = (c.VehicleModel ?? "").ToUpper();
            string dbModel   = modelUpper.Contains("RUV")   ? "RUV350"
                             : modelUpper.Contains("C12")   ? "C12i"
                             : modelUpper.Contains("OOWAH") ? "OOWAH" : "C12i";
            string dbVariant = !string.IsNullOrWhiteSpace(c.VehicleVariant) ? c.VehicleVariant
                             : modelUpper.Contains("MAX 3") ? "Max 3.0"
                             : modelUpper.Contains("MAX 2") ? "Max 2.0"
                             : modelUpper.Contains("MAX")   ? "Max" : "Ex";

            var year = c.YearOfPurchase;
            var basePriceRow = await _db.ExchangeModelBasePrices
                .Where(p => p.ModelName == dbModel && p.VariantName == dbVariant)
                .OrderBy(p => Math.Abs(p.Year - year))
                .FirstOrDefaultAsync();

            if (basePriceRow == null)
                return BadRequest(new
                {
                    error = $"No base price found for model '{dbModel}', variant '{dbVariant}', year {year}. " +
                            $"Please check ExchangeModelBasePrices table."
                });

            var basePrice  = basePriceRow.BasePrice;
            var scrapValue = basePriceRow.ScrapValue;

            // ── 4. KM deduction ─────────────────────────────────────
            var km = c.KmDriven;
            var kmSlab = await _db.ExchangeKmSlabs
                .Where(s => s.KmFrom <= km && (s.KmTo == null || s.KmTo >= km))
                .FirstOrDefaultAsync();
            var kmDeduction = kmSlab?.Deduction ?? 0;

            // ── 5. Condition score (excluding Battery category) ─────
            var allScores = c.ExchangeInspectionScores.ToList();

            var conditionScore = (decimal)(allScores
                .Where(s => s.Category != "Battery")
                .Select(s => (double)s.Score)
                .DefaultIfEmpty(5)
                .Average());

            var condSlab = await _db.ExchangeConditionSlabs
                .Where(s => s.ScoreFrom <= conditionScore && s.ScoreTo >= conditionScore)
                .FirstOrDefaultAsync();
            var conditionAdjustment = condSlab?.Adjustment ?? 0;

            // ── 6. Battery score adjustment ──────────────────────────
            var batteryScore = (decimal)(allScores
                .Where(s => s.Category == "Battery")
                .Select(s => (double)s.Score)
                .DefaultIfEmpty(5)
                .Average());

            var batSlab = await _db.ExchangeBatterySlabs
                .Where(s => s.ScoreFrom <= batteryScore && s.ScoreTo >= batteryScore)
                .FirstOrDefaultAsync();
            var batteryAdjustment = batSlab?.Adjustment ?? 0;

            // ── 7. Config values ─────────────────────────────────────
            var configs = await _db.ExchangePricingConfigs.ToListAsync();
            decimal GetConfig(string key, decimal def) => configs.FirstOrDefault(cfg => cfg.ConfigKey == key)?.ConfigValue ?? def;

            var margin        = GetConfig("Margin",            5000m);
            var rangeLowerPct = GetConfig("RangeLowerPct",      0.90m);
            var rangeUpperPct = GetConfig("RangeUpperPct",      1.10m);
            var refurbishment = GetConfig("RefurbishmentCost",  300m);

            // ── 8. Final price formula ───────────────────────────────
            var rawPrice = basePrice
                        - kmDeduction
                        + conditionAdjustment
                        + batteryAdjustment
                        - margin
                        - refurbishment;

            var finalPrice = Math.Max(rawPrice, scrapValue);   // floor = scrap value
            finalPrice     = Math.Min(finalPrice, basePrice);  // ceiling = base price
            finalPrice     = Math.Round(finalPrice / 100) * 100;

            var minPrice   = Math.Round(finalPrice * rangeLowerPct / 100) * 100;
            var maxPrice = Math.Round(finalPrice * rangeUpperPct / 100) * 100;

            // ── 9. Grade ─────────────────────────────────────────────
            var totalScore = c.TotalScore ?? 5m;
            var grade = totalScore >= 8 ? "Excellent"
                    : totalScore >= 5 ? "Good"
                    : "Average";

            c.RecommendedPrice = finalPrice;
            c.MinPrice         = minPrice;
            c.MaxPrice         = maxPrice;
            c.Grade            = grade;
            c.Status           = "ImagesPending";
            c.UpdatedAt        = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Priced case {CaseId}: model={Model} variant={Variant} year={Year} " +
                "base={Base} kmDed={KM} cond={Cond} bat={Bat} margin={Margin} refurb={Refurb} " +
                "→ final={Final} [{Min}–{Max}]",
                id, dbModel, dbVariant, year,
                basePrice, kmDeduction, conditionAdjustment,
                batteryAdjustment, margin, refurbishment,
                finalPrice, minPrice, maxPrice);

            return Ok(new
            {
                recommended = finalPrice,
                minPrice,
                maxPrice,
                grade,
                totalScore  = c.TotalScore,
                breakdown   = new
                {
                    basePrice,
                    kmDeduction,
                    conditionAdjustment,
                    batteryAdjustment,
                    margin,
                    refurbishment,
                    scrapValue,
                    rawPrice,
                }
            });
        }

        // ── POST /api/ExchangeCases/{id}/submit ───────────────────────────────
        [HttpPost("{id}/submit")]
        public async Task<IActionResult> Submit(int id)
        {
            var c = await _db.ExchangeCases
                .Include(x => x.ExchangeCaseImages)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (c == null) return NotFound();
            if (c.DealerId != CurrentUser && !IsAdmin) return Forbid();

            var required = new[] { "Front", "Rear", "Left", "Right", "Odometer", "Battery" };
            if (required.Except(c.ExchangeCaseImages.Select(i => i.ImageType).ToHashSet()).Any())
                return BadRequest("All 6 images must be uploaded before submission.");

            if (c.RecommendedPrice == null)
                return BadRequest("Price range must be generated before submission.");

            c.Status      = "PendingAdminReview";
            c.SubmittedAt = DateTime.UtcNow;
            c.UpdatedAt   = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // ── Resolve full User — FullName used in email greetings ───────────
            var dealer       = await GetCurrentUserAsync();
            var caseSnapshot = c;

            _ = Task.Run(async () =>
            {
                try { await _email.SendCaseSubmissionEmailsAsync(caseSnapshot, dealer); }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Submission emails failed for {Code}", c.CaseNumber);
                }
            });

            return Ok(new { caseNumber = c.CaseNumber, status = c.Status });
        }

        // ── POST /api/ExchangeCases/{id}/admin-action ─────────────────────────
        [HttpPost("{id}/admin-action")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AdminAction(int id, [FromBody] AdminActionDto dto)
        {
            var c = await _db.ExchangeCases.FindAsync(id);
            if (c == null) return NotFound();
            if (c.Status != "PendingAdminReview") return BadRequest("Case is not pending review.");

            var validActions = new[] { "Approved", "Modified", "Rejected" };
            if (!validActions.Contains(dto.Action)) return BadRequest("Invalid action.");

            c.Status        = dto.Action == "Approved" ? "AdminApproved"
                            : dto.Action == "Modified" ? "AdminModified"
                            : "AdminRejected";
            c.AdminNote     = dto.Note;
            c.ApprovedPrice = dto.Action == "Rejected" ? null : dto.Price;
            c.AdminActionAt = DateTime.UtcNow;
            c.UpdatedAt     = DateTime.UtcNow;

            _db.ExchangeAdminActions.Add(new ExchangeAdminAction
                { CaseId = id, AdminUser = CurrentUser, Action = dto.Action,
                  PriceSet = dto.Price, Note = dto.Note });

            await _db.SaveChangesAsync();

            // ── Resolve dealer's full User from DealerId on the case ───────────
            var dealer       = await ResolveDealerAsync(c.DealerId);
            var action       = dto.Action;
            var note         = dto.Note;
            var caseSnapshot = c;

            _ = Task.Run(async () =>
            {
                try { await _email.SendAdminActionEmailAsync(caseSnapshot, dealer, action, note); }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Decision email failed for {Code}", c.CaseNumber);
                }
            });

            return Ok(new { status = c.Status, approvedPrice = c.ApprovedPrice });
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static (decimal score, string grade) ComputeScore(List<ScoreDto> scores)
        {
            if (!scores.Any()) return (0, "Average");
            decimal weighted = 0;
            foreach (var g in scores.GroupBy(s => s.Category))
                weighted += g.Average(x => (decimal)x.Score) *
                    (CategoryWeights.TryGetValue(g.Key, out var w) ? w : 0.05m);
            return (Math.Round(weighted, 2),
                weighted >= 8 ? "Excellent" : weighted >= 5 ? "Good" : "Average");
        }
    }
}