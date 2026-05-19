using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class ScootyInventory
{
    public int ScootyId { get; set; }

    public int ModelId { get; set; }

    public int VariantId { get; set; }

    public int? ColourId { get; set; }

    public decimal? Price { get; set; }

    public string? BatterySpecs { get; set; }

    public int? RangeKm { get; set; }

    public bool StockAvailable { get; set; }

    public string? ImageUrl { get; set; }

    public decimal? MaxPowerKw { get; set; }

    public string? BrakeFront { get; set; }

    public string? BrakeRear { get; set; }

    public string? BrakingType { get; set; }

    public string? WheelSize { get; set; }

    public string? WheelType { get; set; }

    public string? ChargingTimeHrs { get; set; }

    public string? StartingType { get; set; }

    public string? Speedometer { get; set; }

    public int StockQuantity { get; set; }

    public virtual ICollection<AreaScootyStock> AreaScootyStocks { get; set; } = new List<AreaScootyStock>();

    public virtual VehicleColour? Colour { get; set; }

    public virtual ICollection<ComparisonConfig> ComparisonConfigScooty1s { get; set; } = new List<ComparisonConfig>();

    public virtual ICollection<ComparisonConfig> ComparisonConfigScooty2s { get; set; } = new List<ComparisonConfig>();

    public virtual ICollection<ComparisonConfig> ComparisonConfigScooty3s { get; set; } = new List<ComparisonConfig>();

    public virtual ICollection<EmiEnquiry> EmiEnquiries { get; set; } = new List<EmiEnquiry>();

    public virtual VehicleModel Model { get; set; } = null!;

    public virtual ICollection<RoadPrice> RoadPrices { get; set; } = new List<RoadPrice>();

    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();

    public virtual ScootySpec? ScootySpec { get; set; }

    public virtual ICollection<UserLike> UserLikes { get; set; } = new List<UserLike>();

    public virtual VehicleVariant Variant { get; set; } = null!;

    public virtual ICollection<VehicleReview> VehicleReviews { get; set; } = new List<VehicleReview>();
}
