using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class EmiEnquiry
{
    public int Id { get; set; }

    public int ScootyId { get; set; }

    public string UserId { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string MobileNumber { get; set; } = null!;

    public string PinCode { get; set; } = null!;

    public bool WantsLoan { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual ScootyInventory Scooty { get; set; } = null!;
}
