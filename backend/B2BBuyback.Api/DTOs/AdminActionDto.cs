namespace B2BBuyback.Api.DTOs;

public class AdminActionDto
{
    public string  Action { get; set; } = string.Empty;
    public decimal? Price  { get; set; }
    public string?  Note   { get; set; }
}