using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class VwAreaStockSummary
{
    public int Id { get; set; }

    public int ScootyId { get; set; }

    public string ModelName { get; set; } = null!;

    public string? VariantName { get; set; }

    public string? ColourName { get; set; }

    public string? ImageUrl { get; set; }

    public decimal? Price { get; set; }

    public int? RangeKm { get; set; }

    public int StockQuantity { get; set; }

    public bool? StockAvailable { get; set; }

    public int CityAreaId { get; set; }

    public string AreaName { get; set; } = null!;

    public string Pincode { get; set; } = null!;

    public int CityId { get; set; }

    public string CityName { get; set; } = null!;

    public string StateName { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }
}
