using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class AreaScootyStock
{
    public int Id { get; set; }

    public int ScootyId { get; set; }

    public int CityAreaId { get; set; }

    public int StockQuantity { get; set; }

    public bool? StockAvailable { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CityArea CityArea { get; set; } = null!;

    public virtual ScootyInventory Scooty { get; set; } = null!;
}
