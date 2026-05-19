using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class PriceMaster
{
    public int PriceId { get; set; }

    public int ModelId { get; set; }

    public int VariantId { get; set; }

    public DateOnly EffectiveStartDate { get; set; }

    public DateOnly? EffectiveEndDate { get; set; }

    public decimal Price { get; set; }

    public string? PriceListFile { get; set; }

    public virtual VehicleModel Model { get; set; } = null!;

    public virtual VehicleVariant Variant { get; set; } = null!;
}
