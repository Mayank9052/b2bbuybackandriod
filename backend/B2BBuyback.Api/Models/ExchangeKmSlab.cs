using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class ExchangeKmSlab
{
    public int Id { get; set; }

    public int KmFrom { get; set; }

    public int? KmTo { get; set; }

    public decimal Deduction { get; set; }

    public string Label { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
