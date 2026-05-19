namespace B2BBuyback.Api.DTOs;
public class AdminDecisionDto
{
    public string   Action { get; set; } = string.Empty; // Approved|Modified|Rejected
    public decimal? Price  { get; set; }
    public string?  Note   { get; set; }
}