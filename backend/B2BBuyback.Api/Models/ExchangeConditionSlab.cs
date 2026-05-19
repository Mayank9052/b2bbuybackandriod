using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class ExchangeConditionSlab
{
    public int Id { get; set; }

    public decimal ScoreFrom { get; set; }

    public decimal ScoreTo { get; set; }

    public decimal Adjustment { get; set; }

    public string Label { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}
