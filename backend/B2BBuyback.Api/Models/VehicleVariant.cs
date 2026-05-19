using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class VehicleVariant
{
    public int Id { get; set; }

    public string? VariantName { get; set; }

    public int? ModelId { get; set; }

    public virtual VehicleModel Model { get; set; } = null!;

    public virtual ICollection<PriceMaster> PriceMasters { get; set; } = new List<PriceMaster>();

    public virtual ICollection<ScootyInventory> ScootyInventories { get; set; } = new List<ScootyInventory>();
}
