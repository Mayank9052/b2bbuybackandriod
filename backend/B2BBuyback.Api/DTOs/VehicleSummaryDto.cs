namespace B2BBuyback.Api.DTOs;

public class VehicleSummaryDto
{
    public int    CaseId          { get; set; }
    public string CaseNumber      { get; set; } = null!;
    public string CustomerName    { get; set; } = null!;
    public string VehicleModel    { get; set; } = null!;
    public string VehicleVariant  { get; set; } = null!;
    public string RegistrationNo  { get; set; } = null!;
    public string Status          { get; set; } = null!;
    public DateTime? SubmittedAt  { get; set; }
    public decimal? ApprovedPrice { get; set; }
}
