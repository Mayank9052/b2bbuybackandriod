using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using B2BBuyback.Api.Data;
using B2BBuyback.Api.Models;
using B2BBuyback.Api.DTOs;

namespace B2BBuyback.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DealerAuthController : ControllerBase
    {
        private readonly AppDbContext    _context;
        private readonly IConfiguration _configuration;

        // ── In dev mode, this OTP always works ───────────────────
        private const string DEV_OTP = "123456";
        private const bool   IS_DEV  = true;   // flip to false in production

        public DealerAuthController(AppDbContext context, IConfiguration configuration)
        {
            _context       = context;
            _configuration = configuration;
        }

        // ══════════════════════════════════════════════════════════
        // POST api/DealerAuth/send-otp
        // Validates mobile + dealer code, generates OTP
        // ══════════════════════════════════════════════════════════
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            // ── 1. Validate input ─────────────────────────────────
            if (string.IsNullOrWhiteSpace(request.MobileNumber) ||
                request.MobileNumber.Length != 10 ||
                !request.MobileNumber.All(char.IsDigit))
                return BadRequest(new { error = "Enter a valid 10-digit mobile number." });

            if (string.IsNullOrWhiteSpace(request.DealerCode))
                return BadRequest(new { error = "Dealer code is required." });

            // ── 2. Look up dealer ─────────────────────────────────
            var dealer = await _context.Dealers.FirstOrDefaultAsync(d =>
                d.MobileNumber == request.MobileNumber &&
                d.DealerCode   == request.DealerCode.Trim().ToUpper() &&
                d.IsActive);

            if (dealer == null)
                return BadRequest(new { error = "Code not found — contact BGauss." });

            // ── 3. Rate-limit: max 3 OTP requests per 10 min ─────
            if (dealer.OtpLockedUntil.HasValue && dealer.OtpLockedUntil > DateTime.UtcNow)
            {
                var remaining = (int)(dealer.OtpLockedUntil.Value - DateTime.UtcNow).TotalSeconds;
                return BadRequest(new { error = $"Too many attempts. Try again in {remaining} seconds." });
            }

            // ── 4. Generate OTP ───────────────────────────────────
            var otp = IS_DEV ? DEV_OTP : GenerateOtp();

            dealer.OtpCode        = otp;
            dealer.OtpExpiry      = DateTime.UtcNow.AddMinutes(5);
            dealer.OtpAttempts    = 0;
            dealer.OtpLockedUntil = null;
            dealer.UpdatedAt      = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // ── 5. In production: send SMS here ───────────────────
            // await _smsService.SendAsync("+91" + dealer.MobileNumber,
            //     $"Your BGauss OTP is {otp}. Valid for 5 minutes. Do not share.");

            var response = new SendOtpResponse
            {
                Success   = true,
                Message   = $"OTP sent to +91 {dealer.MobileNumber.Substring(0, 5)}XXXXX",
                ExpiresIn = 300,
                DevOtp    = IS_DEV ? otp : null,  // remove in production
            };

            return Ok(response);
        }

        // ══════════════════════════════════════════════════════════
        // POST api/DealerAuth/verify-otp
        // Verifies OTP, returns JWT + refresh token
        // ══════════════════════════════════════════════════════════
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.OtpCode))
                return BadRequest(new { error = "OTP is required." });

            var dealer = await _context.Dealers.FirstOrDefaultAsync(d =>
                d.MobileNumber == request.MobileNumber &&
                d.DealerCode   == request.DealerCode.Trim().ToUpper() &&
                d.IsActive);

            if (dealer == null)
                return BadRequest(new { error = "Dealer not found." });

            // ── Lock check ────────────────────────────────────────
            if (dealer.OtpLockedUntil.HasValue && dealer.OtpLockedUntil > DateTime.UtcNow)
                return BadRequest(new { error = "Account temporarily locked. Try again later." });

            // ── Expiry check ──────────────────────────────────────
            if (!dealer.OtpExpiry.HasValue || dealer.OtpExpiry < DateTime.UtcNow)
                return BadRequest(new { error = "OTP has expired. Please request a new one." });

            // ── OTP match ─────────────────────────────────────────
            if (dealer.OtpCode != request.OtpCode.Trim())
            {
                dealer.OtpAttempts++;
                if (dealer.OtpAttempts >= 3)
                {
                    dealer.OtpLockedUntil = DateTime.UtcNow.AddMinutes(10);
                    dealer.OtpCode        = null;
                }
                await _context.SaveChangesAsync();

                int remaining = Math.Max(0, 3 - dealer.OtpAttempts);
                return BadRequest(new { error = $"Invalid OTP. {remaining} attempt(s) left." });
            }

            // ── Success — clear OTP, generate tokens ──────────────
            dealer.OtpCode         = null;
            dealer.OtpExpiry       = null;
            dealer.OtpAttempts     = 0;
            dealer.OtpLockedUntil  = null;

            var refreshToken       = Guid.NewGuid().ToString("N");
            dealer.RefreshToken        = refreshToken;
            dealer.RefreshTokenExpiry  = DateTime.UtcNow.AddDays(7);
            dealer.UpdatedAt           = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var jwt = GenerateJwt(dealer);

            return Ok(new DealerAuthResponse
            {
                Success      = true,
                Token        = jwt,
                RefreshToken = refreshToken,
                ExpiresIn    = 7200,
                Dealer = new DealerProfileDto
                {
                    DealerId   = dealer.DealerId,
                    DealerCode = dealer.DealerCode,
                    DealerName = dealer.DealerName,
                    Mobile     = dealer.MobileNumber,
                    City       = dealer.City,
                    State      = dealer.State,
                }
            });
        }

        // ══════════════════════════════════════════════════════════
        // POST api/DealerAuth/refresh
        // ══════════════════════════════════════════════════════════
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
        {
            var dealer = await _context.Dealers.FirstOrDefaultAsync(d =>
                d.RefreshToken == request.RefreshToken &&
                d.RefreshTokenExpiry > DateTime.UtcNow &&
                d.IsActive);

            if (dealer == null)
                return Unauthorized(new { error = "Invalid or expired refresh token." });

            // Rotate refresh token
            dealer.RefreshToken       = Guid.NewGuid().ToString("N");
            dealer.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            dealer.UpdatedAt          = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new DealerAuthResponse
            {
                Success      = true,
                Token        = GenerateJwt(dealer),
                RefreshToken = dealer.RefreshToken,
                ExpiresIn    = 7200,
                Dealer = new DealerProfileDto
                {
                    DealerId   = dealer.DealerId,
                    DealerCode = dealer.DealerCode,
                    DealerName = dealer.DealerName,
                    Mobile     = dealer.MobileNumber,
                    City       = dealer.City,
                    State      = dealer.State,
                }
            });
        }

        // ══════════════════════════════════════════════════════════
        // POST api/DealerAuth/logout
        // ══════════════════════════════════════════════════════════
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
        {
            var dealer = await _context.Dealers.FirstOrDefaultAsync(d =>
                d.RefreshToken == request.RefreshToken);

            if (dealer != null)
            {
                dealer.RefreshToken       = null;
                dealer.RefreshTokenExpiry = null;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Logged out successfully." });
        }

        // ── Helpers ───────────────────────────────────────────────
        private string GenerateOtp()
        {
            var rng = new Random();
            return rng.Next(100000, 999999).ToString();
        }

        private string GenerateJwt(Dealer dealer)
        {
            var jwt     = _configuration.GetSection("JwtSettings");
            var key     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds   = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("DealerId",   dealer.DealerId.ToString()),
                new Claim("DealerCode", dealer.DealerCode),
                new Claim(ClaimTypes.Name, dealer.DealerName),
                new Claim(ClaimTypes.Role, "Dealer"),
                new Claim(ClaimTypes.MobilePhone, dealer.MobileNumber),
            };

            var token = new JwtSecurityToken(
                issuer:             jwt["Issuer"],
                audience:           jwt["Audience"],
                claims:             claims,
                expires:            DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
