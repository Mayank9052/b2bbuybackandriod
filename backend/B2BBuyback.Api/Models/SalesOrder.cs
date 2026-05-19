using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class SalesOrder
{
    public int OrderId { get; set; }

    public int CustomerId { get; set; }

    public int ScootyId { get; set; }

    public DateOnly? OrderDate { get; set; }

    public int Quantity { get; set; }

    public decimal? TotalAmount { get; set; }

    public virtual B2bcustomer Customer { get; set; } = null!;

    public virtual ScootyInventory Scooty { get; set; } = null!;
}
