namespace B2BBuyback.Api.DTOs;

public class DealerDashboardDto
{
    public int ProcuredThisMonth  { get; set; }
    public int PendingApproval    { get; set; }
    public int SoldAndSettled     { get; set; }
    public int InRefurbishment    { get; set; }
    public int Listed             { get; set; }
    public List<VehicleSummaryDto> RecentVehicles { get; set; } = new();
}
