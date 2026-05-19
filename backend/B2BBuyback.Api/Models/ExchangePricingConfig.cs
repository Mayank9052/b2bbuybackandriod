using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class ExchangePricingConfig
{
    public int Id { get; set; }

    public string ConfigKey { get; set; } = null!;

    public decimal ConfigValue { get; set; }

    public string Description { get; set; } = null!;

    public DateTime UpdatedAt { get; set; }
}
