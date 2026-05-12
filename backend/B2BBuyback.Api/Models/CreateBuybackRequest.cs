using System.ComponentModel.DataAnnotations;

namespace B2BBuyback.Api.Models;

public sealed class CreateBuybackRequest
{
    [Required]
    [MinLength(2)]
    public string VendorName { get; init; } = string.Empty;

    [Required]
    [MinLength(2)]
    public string DeviceModel { get; init; } = string.Empty;

    [Range(1, 10000)]
    public int Quantity { get; init; }

    [Range(0, 100000000)]
    public decimal ExpectedOfferAmount { get; init; }

    public DateTime PickupWindowUtc { get; init; }
}
