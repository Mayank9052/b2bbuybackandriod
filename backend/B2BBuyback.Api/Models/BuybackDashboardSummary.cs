namespace B2BBuyback.Api.Models;

public sealed class BuybackDashboardSummary
{
    public required int PendingRequests { get; init; }

    public required int InspectionsScheduled { get; init; }

    public required int DevicesQuoted { get; init; }

    public required int PayoutsDue { get; init; }

    public required decimal TotalQuotedValue { get; init; }

    public required DateTime LastUpdatedUtc { get; init; }
}
