using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class ExchangeInspectionScore
{
    public int Id { get; set; }

    public int CaseId { get; set; }

    public string Category { get; set; } = null!;

    public string Parameter { get; set; } = null!;

    public int Score { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ExchangeCase Case { get; set; } = null!;
}
