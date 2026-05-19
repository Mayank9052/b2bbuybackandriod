namespace B2BBuyback.Api.DTOs;

public class VerifyOtpRequest
{
    public string MobileNumber { get; set; } = null!;
    public string DealerCode   { get; set; } = null!;
    public string OtpCode      { get; set; } = null!;
}