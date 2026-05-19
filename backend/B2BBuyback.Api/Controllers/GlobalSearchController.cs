using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using B2BBuyback.Api.Data;

[Route("api/[controller]")]
[ApiController]
public class GlobalSearchController : ControllerBase
{
    private readonly AppDbContext _context;

    public GlobalSearchController(AppDbContext context)
    {
        _context = context;
    }

    // =============================
    // GLOBAL SEARCH
    // =============================
    [HttpGet]
    public async Task<IActionResult> GlobalSearch(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return BadRequest("Search query required");

        query = query.ToLower();

        // =============================
        // SCOOTY SEARCH
        // =============================
        var scooties = await _context.ScootyInventories
            .Include(x => x.Model)
            .Include(x => x.Variant)
            .Include(x => x.Colour)
            .Where(x =>
                (x.Model != null && x.Model.ModelName != null && x.Model.ModelName.ToLower().Contains(query)) ||
                (x.Variant != null && x.Variant.VariantName != null && x.Variant.VariantName.ToLower().Contains(query)) ||
                (x.Colour != null && x.Colour.ColourName != null && x.Colour.ColourName.ToLower().Contains(query)) ||
                (x.BatterySpecs != null && x.BatterySpecs.ToLower().Contains(query))
            )
            .Select(x => new
            {
                Type = "Scooty",
                Id = x.ScootyId,
                Name = x.Model.ModelName + " " + x.Variant.VariantName,
                Price = x.Price,
                Image = x.ImageUrl
            })
            .ToListAsync();

        // =============================
        // CUSTOMER SEARCH
        // =============================
        var customers = await _context.B2bcustomers
            .Where(x =>
                x.CompanyName.ToLower().Contains(query) ||
                (x.ContactPerson != null && x.ContactPerson.ToLower().Contains(query)) ||
                (x.Email != null && x.Email.ToLower().Contains(query))
            )
            .Select(x => new
            {
                Type = "Customer",
                Id = x.CustomerId,
                Name = x.CompanyName +
                       (x.ContactPerson != null ? " (" + x.ContactPerson + ")" : ""),
                Phone = x.Phone
            })
            .ToListAsync();

        // =============================
        // ORDER SEARCH
        // =============================
        var orders = await _context.SalesOrders
            .Include(x => x.Customer)
            .Include(x => x.Scooty)
                .ThenInclude(s => s.Model)
            .Where(x =>
                (x.Customer != null &&
                 x.Customer.CompanyName != null &&
                 x.Customer.CompanyName.ToLower().Contains(query)) ||

                (x.Scooty != null &&
                 x.Scooty.Model != null &&
                 x.Scooty.Model.ModelName != null &&
                 x.Scooty.Model.ModelName.ToLower().Contains(query))
            )
            .Select(x => new
            {
                Type = "Order",
                Id = x.OrderId,
                Customer = x.Customer.CompanyName,
                Model = x.Scooty.Model.ModelName,
                x.Quantity,
                x.TotalAmount
            })
            .ToListAsync();

        // =============================
        // PRICE SEARCH
        // =============================
        var prices = await _context.PriceMasters
            .Include(x => x.Model)
            .Include(x => x.Variant)
            .Where(x =>
                (x.Model != null && x.Model.ModelName != null && x.Model.ModelName.ToLower().Contains(query)) ||
                (x.Variant != null && x.Variant.VariantName != null && x.Variant.VariantName.ToLower().Contains(query))
            )
            .Select(x => new
            {
                Type = "Price",
                Id = x.PriceId,
                Name = x.Model.ModelName + " " + x.Variant.VariantName,
                x.Price
            })
            .ToListAsync();

        // =============================
        // FINAL COMBINED RESULT
        // =============================
        var result = new
        {
            Scooties = scooties,
            Customers = customers,
            Orders = orders,
            Prices = prices
        };

        return Ok(result);
    }
}