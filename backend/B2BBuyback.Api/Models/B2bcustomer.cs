using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class B2bcustomer
{
    public int CustomerId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string? ContactPerson { get; set; }

    public string? Address { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Gstnumber { get; set; }

    public string? LogoPath { get; set; }

    public virtual ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
