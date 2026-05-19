namespace B2BBuyback.Api.DTOs;

public class SubmitReviewDto
{
    public int ScootyId { get; set; }
    public string UserId { get; set; } = "";
    public string Title { get; set; } = "";
    public string ReviewText { get; set; } = "";
    public int Rating { get; set; }
    public int? PerformanceRating { get; set; }
    public int? MileageRating { get; set; }
    public int? ComfortRating { get; set; }
    public int? MaintenanceRating { get; set; }
    public int? FeaturesRating { get; set; }
}