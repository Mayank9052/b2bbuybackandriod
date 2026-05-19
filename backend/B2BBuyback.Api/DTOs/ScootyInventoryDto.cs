namespace B2BBuyback.Api.DTOs;

public class ScootyInventoryDto
{
    public int ScootyId { get; set; }
    public string ModelName { get; set; } = null!;
    public string VariantName { get; set; } = null!;
    public string? ColourName { get; set; }
    public decimal? Price { get; set; }
    public bool StockAvailable { get; set; }
    public string? ImageUrl { get; set; }
    public ScootySpecDto? ScootySpec { get; set; }
}