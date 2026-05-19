using System;

namespace B2BBuyback.Api.Models;

public class Dealer
{
    public int DealerId { get; set; }
    public string DealerCode { get; set; } = null!;   // e.g. BG-PUN-001
    public string DealerName { get; set; } = null!;
    public string MobileNumber { get; set; } = null!;  // 10-digit, no +91
    public string? Email { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // OTP fields
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiry { get; set; }
    public int OtpAttempts { get; set; } = 0;
    public DateTime? OtpLockedUntil { get; set; }

    // Session
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
}
