using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class ExchangeAdminAction
{
    public int Id { get; set; }

    public int CaseId { get; set; }

    public string AdminUser { get; set; } = null!;

    public string Action { get; set; } = null!;

    public decimal? PriceSet { get; set; }

    public string? Note { get; set; }

    public DateTime ActionAt { get; set; }

    public virtual ExchangeCase Case { get; set; } = null!;
}
