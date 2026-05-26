// B2BBuyback.Api/Models/Dealer.cs
// Replace your existing Dealer.cs with this — matches AppDbContext scaffold
// + adds the fields DealerAuthController needs

using System;

namespace B2BBuyback.Api.Models;

public partial class Dealer
{
    public int DealerId { get; set; }

    public string DealerCode { get; set; } = null!;

    public string DealerName { get; set; } = null!;

    public string MobileNumber { get; set; } = null!;

    public string? Email { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    // ── OTP fields (new columns — run AddMissingDealerColumns.sql) ──
    public string? OtpCode { get; set; }

    public DateTime? OtpExpiry { get; set; }

    public int OtpAttempts { get; set; }

    public DateTime? OtpLockedUntil { get; set; }

    // ── Session fields (new columns) ─────────────────────────────
    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiry { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
