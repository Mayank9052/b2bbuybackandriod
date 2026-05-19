namespace B2BBuyback.Api.DTOs;
public class SendOtpResponse
{
    public bool   Success   { get; set; }
    public string Message   { get; set; } = null!;
    public int    ExpiresIn { get; set; } = 300; // seconds (5 min)
    // In dev, return OTP directly so Flutter can auto-fill
    public string? DevOtp   { get; set; }
}
