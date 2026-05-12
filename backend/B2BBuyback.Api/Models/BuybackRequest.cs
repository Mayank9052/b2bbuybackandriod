namespace B2BBuyback.Api.Models;

public sealed class BuybackRequest
{
    public required string Id { get; init; }

    public required string VendorName { get; init; }

    public required string DeviceModel { get; init; }

    public required string Status { get; set; }

    public required int Quantity { get; init; }

    public required decimal QuotedAmount { get; set; }

    public required DateTime PickupWindowUtc { get; init; }

    public required DateTime CreatedUtc { get; init; }
}
