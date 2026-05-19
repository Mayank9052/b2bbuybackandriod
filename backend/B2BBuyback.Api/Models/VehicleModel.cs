using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class VehicleModel
{
    public int Id { get; set; }

    public string ModelName { get; set; } = null!;

    public virtual ICollection<PriceMaster> PriceMasters { get; set; } = new List<PriceMaster>();

    public virtual ICollection<ScootyInventory> ScootyInventories { get; set; } = new List<ScootyInventory>();

    public virtual ICollection<VehicleBrochure> VehicleBrochures { get; set; } = new List<VehicleBrochure>();
}
