using B2BBuyback.Api.Models;

namespace B2BBuyback.Api.Services;

public interface IBuybackService
{
    BuybackDashboardSummary GetSummary();

    IReadOnlyList<BuybackRequest> GetRequests();

    BuybackRequest? GetRequestById(string id);

    BuybackRequest CreateRequest(CreateBuybackRequest request);
}
