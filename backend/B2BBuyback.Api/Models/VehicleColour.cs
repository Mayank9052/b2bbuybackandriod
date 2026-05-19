using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class VehicleColour
{
    public int Id { get; set; }

    public string? ColourName { get; set; }

    public int? ModelId { get; set; }

    public int? VariantId { get; set; }

    public string? HexCode { get; set; }

    public virtual ICollection<ScootyInventory> ScootyInventories { get; set; } = new List<ScootyInventory>();
}
