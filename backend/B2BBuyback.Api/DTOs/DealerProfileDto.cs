namespace B2BBuyback.Api.DTOs;
public class DealerProfileDto
{
    public int    DealerId   { get; set; }
    public string DealerCode { get; set; } = null!;
    public string DealerName { get; set; } = null!;
    public string Mobile     { get; set; } = null!;
    public string? City      { get; set; }
    public string? State     { get; set; }
}