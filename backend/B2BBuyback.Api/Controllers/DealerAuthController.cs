// B2BBuyback.Api/Controllers/DealerAuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using B2BBuyback.Api.Data;
using B2BBuyback.Api.Models;
using B2BBuyback.Api.DTOs;
using B2BBuyback.Api.Interfaces;   // ← IDealerOtpEmailService lives here

namespace B2BBuyback.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DealerAuthController : ControllerBase
    {
        private readonly AppDbContext           _context;
        private readonly IConfiguration         _configuration;
        private readonly IDealerOtpEmailService _otpEmail;
        private readonly ILogger<DealerAuthController> _logger;

        // IS_DEV = true  → OTP is always "123456", shown in API response + email sent
        // IS_DEV = false → random 6-digit OTP, only sent via email (production)
        private const bool IS_DEV = true;

        public DealerAuthController(
            AppDbContext context,
            IConfiguration configuration,
            IDealerOtpEmailService otpEmail,
            ILogger<DealerAuthController> logger)
        {
            _context       = context;
            _configuration = configuration;
            _otpEmail      = otpEmail;
            _logger        = logger;
        }

        // ══════════════════════════════════════════════════════════
        // POST api/DealerAuth/send-otp
        // ══════════════════════════════════════════════════════════
        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
        {
            // 1. Validate
            if (string.IsNullOrWhiteSpace(request.MobileNumber) ||
                request.MobileNumber.Length != 10 ||
                !request.MobileNumber.All(char.IsDigit))
                return BadRequest(new { error = "Enter a valid 10-digit mobile number." });

            if (string.IsNullOrWhiteSpace(request.DealerCode))
                return BadRequest(new { error = "Dealer code is required." });

            // 2. Find dealer
            var dealer = await _context.Dealers.FirstOrDefaultAsync(d =>
                d.MobileNumber == request.MobileNumber &&
                d.DealerCode   == request.DealerCode.Trim().ToUpper() &&
                d.IsActive);

            if (dealer == null)
                return BadRequest(new
                {
                    error = "Mobile number or dealer code not found. Contact BGauss support."
                });

            // 3. Rate-limit
            if (dealer.OtpLockedUntil.HasValue && dealer.OtpLockedUntil > DateTime.UtcNow)
            {
                var secs = (int)(dealer.OtpLockedUntil.Value - DateTime.UtcNow).TotalSeconds;
                return BadRequest(new { error = $"Too many attempts. Try again in {secs} seconds." });
            }

            // 4. Generate OTP
            var otp = IS_DEV ? "123456" : GenerateOtp();

            dealer.OtpCode        = otp;
            dealer.OtpExpiry      = DateTime.UtcNow.AddMinutes(5);
            dealer.OtpAttempts    = 0;
            dealer.OtpLockedUntil = null;
            dealer.UpdatedAt      = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // 5. Send OTP email async (fire-and-forget) to avoid blocking API response
            string? emailSentTo = null;
            string  message     = "";

            if (!string.IsNullOrWhiteSpace(dealer.Email))
            {
                emailSentTo = MaskEmail(dealer.Email);
                message = $"OTP sent to {emailSentTo}";
                
                // Send email asynchronously without awaiting (fire-and-forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _otpEmail.SendOtpEmailAsync(
                            toEmail:       dealer.Email,
                            dealerName:    dealer.DealerName,
                            otpCode:       otp,
                            expiryMinutes: 5);

                        _logger.LogInformation(
                            "OTP sent to dealer {Code} at {Email}", dealer.DealerCode, emailSentTo);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Email OTP failed for dealer {Code}", dealer.DealerCode);
                    }
                });
            }
            else
            {
                message = $"OTP generated for +91 {dealer.MobileNumber[..5]}XXXXX " +
                          "(no email registered — add email to Dealers table)";
                _logger.LogWarning(
                    "Dealer {Code} has no email — OTP generated but not emailed", dealer.DealerCode);
            }

            return Ok(new SendOtpResponse
            {
                Success   = true,
                Message   = message,
                ExpiresIn = 300,
                DevOtp    = IS_DEV ? otp : null,   // null in production
            });
        }

        // ══════════════════════════════════════════════════════════
        // POST api/DealerAuth/verify-otp
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

            if (dealer.OtpLockedUntil.HasValue && dealer.OtpLockedUntil > DateTime.UtcNow)
                return BadRequest(new { error = "Account locked. Try again later." });

            if (!dealer.OtpExpiry.HasValue || dealer.OtpExpiry < DateTime.UtcNow)
                return BadRequest(new { error = "OTP has expired. Please request a new one." });

            if (dealer.OtpCode != request.OtpCode.Trim())
            {
                dealer.OtpAttempts++;
                if (dealer.OtpAttempts >= 3)
                {
                    dealer.OtpLockedUntil = DateTime.UtcNow.AddMinutes(10);
                    dealer.OtpCode        = null;
                }
                await _context.SaveChangesAsync();

                int left = Math.Max(0, 3 - dealer.OtpAttempts);
                return BadRequest(new
                {
                    error = left > 0
                        ? $"Invalid OTP. {left} attempt(s) left."
                        : "Too many invalid attempts. Account locked for 10 minutes."
                });
            }

            // ── Success ────────────────────────────────────────────
            dealer.OtpCode        = null;
            dealer.OtpExpiry      = null;
            dealer.OtpAttempts    = 0;
            dealer.OtpLockedUntil = null;

            var refreshToken          = Guid.NewGuid().ToString("N");
            dealer.RefreshToken       = refreshToken;
            dealer.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            dealer.UpdatedAt          = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Dealer {Code} ({Name}) logged in", dealer.DealerCode, dealer.DealerName);

            return Ok(new DealerAuthResponse
            {
                Success      = true,
                Token        = GenerateJwt(dealer),
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

            return Ok(new { message = "Logged out." });
        }

        // ── Helpers ───────────────────────────────────────────────
        private static string GenerateOtp()
        {
            var bytes = new byte[4];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            var n = BitConverter.ToUInt32(bytes, 0) % 900_000 + 100_000;
            return n.ToString();
        }

        private static string MaskEmail(string email)
        {
            var at = email.IndexOf('@');
            if (at <= 1) return email;
            var local  = email[..at];
            var masked = local[0] + new string('*', Math.Max(1, local.Length - 2)) + local[^1];
            return masked + email[at..];
        }

        private string GenerateJwt(Dealer dealer)
        {
            var jwt   = _configuration.GetSection("JwtSettings");
            var key   = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwt["Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim("DealerId",             dealer.DealerId.ToString()),
                new Claim("DealerCode",           dealer.DealerCode),
                new Claim(ClaimTypes.Name,        dealer.DealerName),
                new Claim(ClaimTypes.Role,        "Dealer"),
                new Claim(ClaimTypes.MobilePhone, dealer.MobileNumber),
            };

            var token = new JwtSecurityToken(
                issuer:             jwt["Issuer"],
                audience:           jwt["Audience"],
                claims:             claims,
                expires:            DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}