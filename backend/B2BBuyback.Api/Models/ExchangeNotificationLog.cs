using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class ExchangeNotificationLog
{
    public int Id { get; set; }

    public int CaseId { get; set; }

    public string DealerId { get; set; } = null!;

    public string ActionType { get; set; } = null!;

    public string Message { get; set; } = null!;

    public DateTime SentAt { get; set; }

    public bool IsRead { get; set; }

    public virtual ExchangeCase Case { get; set; } = null!;
}
