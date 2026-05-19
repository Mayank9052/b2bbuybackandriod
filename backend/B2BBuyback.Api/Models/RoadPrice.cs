using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class RoadPrice
{
    public int Id { get; set; }

    public int ScootyId { get; set; }

    public int CityId { get; set; }

    public decimal ExShowroomPrice { get; set; }

    public decimal RtoCharges { get; set; }

    public decimal InsuranceAmount { get; set; }

    public decimal OtherCharges { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual City City { get; set; } = null!;

    public virtual ScootyInventory Scooty { get; set; } = null!;
}
