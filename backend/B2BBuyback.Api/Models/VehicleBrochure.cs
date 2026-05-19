using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class VehicleBrochure
{
    public int Id { get; set; }

    public int ModelId { get; set; }

    public string BrochureUrl { get; set; } = null!;

    public DateTime UploadedAt { get; set; }

    public virtual VehicleModel Model { get; set; } = null!;
}
