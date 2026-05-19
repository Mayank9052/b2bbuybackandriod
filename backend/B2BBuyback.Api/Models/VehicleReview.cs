using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class VehicleReview
{
    public int Id { get; set; }

    public int ScootyId { get; set; }

    public string UserId { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string ReviewText { get; set; } = null!;

    public int Rating { get; set; }

    public int? PerformanceRating { get; set; }

    public int? MileageRating { get; set; }

    public int? ComfortRating { get; set; }

    public int? MaintenanceRating { get; set; }

    public int? FeaturesRating { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsApproved { get; set; }

    public virtual ScootyInventory Scooty { get; set; } = null!;
}
