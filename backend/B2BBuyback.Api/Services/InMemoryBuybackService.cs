using B2BBuyback.Api.Models;

namespace B2BBuyback.Api.Services;

public sealed class InMemoryBuybackService : IBuybackService
{
    private readonly List<BuybackRequest> _requests;
    private DateTime _lastUpdatedUtc;

    public InMemoryBuybackService()
    {
        _lastUpdatedUtc = DateTime.UtcNow;
        _requests =
        [
            new BuybackRequest
            {
                Id = "BB-2401",
                VendorName = "Orbit Devices",
                DeviceModel = "iPhone 13 Pro",
                Status = "Awaiting Pickup",
                Quantity = 12,
                QuotedAmount = 8200,
                PickupWindowUtc = DateTime.UtcNow.AddDays(1),
                CreatedUtc = DateTime.UtcNow.AddHours(-7)
            },
            new BuybackRequest
            {
                Id = "BB-2402",
                VendorName = "Nova Trade",
                DeviceModel = "Samsung S22",
                Status = "Inspection Scheduled",
                Quantity = 7,
                QuotedAmount = 4100,
                PickupWindowUtc = DateTime.UtcNow.AddDays(2),
                CreatedUtc = DateTime.UtcNow.AddHours(-5)
            },
            new BuybackRequest
            {
                Id = "BB-2403",
                VendorName = "Green Loop",
                DeviceModel = "MacBook Air M1",
                Status = "Quoted",
                Quantity = 4,
                QuotedAmount = 9600,
                PickupWindowUtc = DateTime.UtcNow.AddDays(3),
                CreatedUtc = DateTime.UtcNow.AddHours(-2)
            },
            new BuybackRequest
            {
                Id = "BB-2404",
                VendorName = "CellCycle Partners",
                DeviceModel = "iPad Air",
                Status = "Payout Ready",
                Quantity = 6,
                QuotedAmount = 6550,
                PickupWindowUtc = DateTime.UtcNow.AddDays(1),
                CreatedUtc = DateTime.UtcNow.AddHours(-1)
            }
        ];
    }

    public BuybackDashboardSummary GetSummary()
    {
        var pendingRequests = _requests.Count(request => request.Status != "Closed");
        var inspectionsScheduled = _requests.Count(request => request.Status == "Inspection Scheduled");
        var devicesQuoted = _requests
            .Where(request => request.Status is "Quoted" or "Payout Ready")
            .Sum(request => request.Quantity);
        var payoutsDue = _requests.Count(request => request.Status == "Payout Ready");
        var totalQuotedValue = _requests.Sum(request => request.QuotedAmount);

        return new BuybackDashboardSummary
        {
            PendingRequests = pendingRequests,
            InspectionsScheduled = inspectionsScheduled,
            DevicesQuoted = devicesQuoted,
            PayoutsDue = payoutsDue,
            TotalQuotedValue = totalQuotedValue,
            LastUpdatedUtc = _lastUpdatedUtc
        };
    }

    public IReadOnlyList<BuybackRequest> GetRequests()
    {
        return _requests
            .OrderBy(request => request.PickupWindowUtc)
            .ThenBy(request => request.VendorName)
            .ToArray();
    }

    public BuybackRequest? GetRequestById(string id)
    {
        return _requests.FirstOrDefault(
            request => string.Equals(request.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public BuybackRequest CreateRequest(CreateBuybackRequest request)
    {
        var nextNumber = _requests.Count + 2401;
        var buybackRequest = new BuybackRequest
        {
            Id = $"BB-{nextNumber}",
            VendorName = request.VendorName.Trim(),
            DeviceModel = request.DeviceModel.Trim(),
            Status = "New",
            Quantity = request.Quantity,
            QuotedAmount = request.ExpectedOfferAmount,
            PickupWindowUtc = request.PickupWindowUtc == default
                ? DateTime.UtcNow.AddDays(2)
                : request.PickupWindowUtc.ToUniversalTime(),
            CreatedUtc = DateTime.UtcNow
        };

        _requests.Add(buybackRequest);
        _lastUpdatedUtc = DateTime.UtcNow;

        return buybackRequest;
    }
}
