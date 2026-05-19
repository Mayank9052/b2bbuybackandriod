using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class ExchangeModelBasePrice
{
    public int Id { get; set; }

    public string ModelName { get; set; } = null!;

    public string VariantName { get; set; } = null!;

    public int Year { get; set; }

    public decimal BasePrice { get; set; }

    public decimal ScrapValue { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
