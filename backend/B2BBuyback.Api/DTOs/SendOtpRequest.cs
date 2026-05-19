namespace B2BBuyback.Api.DTOs;

public class SendOtpRequest
{
    public string MobileNumber { get; set; } = null!;  // 10-digit, no +91
    public string DealerCode   { get; set; } = null!;
}
