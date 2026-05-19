using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class ComparisonConfig
{
    public int Id { get; set; }

    public int Scooty1Id { get; set; }

    public int Scooty2Id { get; set; }

    public bool IsActive { get; set; }

    public int? Scooty3Id { get; set; }

    public virtual ScootyInventory Scooty1 { get; set; } = null!;

    public virtual ScootyInventory Scooty2 { get; set; } = null!;

    public virtual ScootyInventory? Scooty3 { get; set; }
}
