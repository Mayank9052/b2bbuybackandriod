namespace B2BBuyback.Api.DTOs;

public class BulkUpsertDto
{
    public int ScootyId { get; set; }
    public List<AreaStockItem> Stocks { get; set; } = new();
}