using System;
using System.Collections.Generic;

namespace B2BBuyback.Api.Models;

public partial class ExchangeCase
{
    public int Id { get; set; }

    public string CaseNumber { get; set; } = null!;

    public string CustomerName { get; set; } = null!;

    public string MobileNumber { get; set; } = null!;

    public string City { get; set; } = null!;

    public string VehicleModel { get; set; } = null!;

    public string RegistrationNo { get; set; } = null!;

    public int YearOfPurchase { get; set; }

    public int KmDriven { get; set; }

    public decimal? RecommendedPrice { get; set; }

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public string? Grade { get; set; }

    public decimal? TotalScore { get; set; }

    public string Status { get; set; } = null!;

    public string DealerId { get; set; } = null!;

    public string? AdminNote { get; set; }

    public decimal? ApprovedPrice { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? SubmittedAt { get; set; }

    public DateTime? AdminActionAt { get; set; }

    public string? VehicleVariant { get; set; }

    public virtual ICollection<ExchangeAdminAction> ExchangeAdminActions { get; set; } = new List<ExchangeAdminAction>();

    public virtual ICollection<ExchangeCaseImage> ExchangeCaseImages { get; set; } = new List<ExchangeCaseImage>();

    public virtual ICollection<ExchangeInspectionScore> ExchangeInspectionScores { get; set; } = new List<ExchangeInspectionScore>();

    public virtual ICollection<ExchangeNotificationLog> ExchangeNotificationLogs { get; set; } = new List<ExchangeNotificationLog>();
}
