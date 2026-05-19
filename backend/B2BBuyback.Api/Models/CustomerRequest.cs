using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class CustomerRequest
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = null!;

    public string MobileNumber { get; set; } = null!;

    public string? Email { get; set; }

    public string City { get; set; } = null!;

    public string? State { get; set; }

    public string? Gender { get; set; }

    public string? PreferredModel { get; set; }

    public string? RequestType { get; set; }

    public string? PreferredContact { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = null!;
}
