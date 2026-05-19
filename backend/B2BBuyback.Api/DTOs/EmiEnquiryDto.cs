namespace B2BBuyback.Api.DTOs;

public class EmiEnquiryDto
{
    public int ScootyId { get; set; }
    public string? UserId { get; set; }
    public string FullName { get; set; } = "";
    public string MobileNumber { get; set; } = "";
    public string PinCode { get; set; } = "";
    public bool WantsLoan { get; set; }
}