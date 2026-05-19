using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using B2BBuyback.Api.Data;
using B2BBuyback.Api.Models;

[Route("api/[controller]")]
[ApiController]
public class SalesOrderController : ControllerBase
{
    private readonly AppDbContext _context;

    public SalesOrderController(AppDbContext context)
    {
        _context = context;
    }

    // =============================
    // GET ALL
    // =============================
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _context.SalesOrders
            .Include(x => x.Customer)
            .Include(x => x.Scooty)
            .ThenInclude(s => s.Model)
            .Select(x => new
            {
                x.OrderId,
                CustomerName = x.Customer.CompanyName +
    (x.Customer.ContactPerson != null ? " (" + x.Customer.ContactPerson + ")" : ""),
                ModelName = x.Scooty.Model.ModelName,
                x.Quantity,
                x.TotalAmount,
                x.OrderDate
            })
            .ToListAsync();

        return Ok(data);
    }

    // =============================
    // GET BY ID
    // =============================
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _context.SalesOrders
            .Include(x => x.Customer)
            .Include(x => x.Scooty)
            .ThenInclude(s => s.Model)
            .Where(x => x.OrderId == id)
            .Select(x => new
            {
                x.OrderId,
                CustomerName = x.Customer.CompanyName +
    (x.Customer.ContactPerson != null ? " (" + x.Customer.ContactPerson + ")" : ""),
                ModelName = x.Scooty.Model.ModelName,
                x.Quantity,
                x.TotalAmount,
                x.OrderDate
            })
            .FirstOrDefaultAsync();

        if (data == null)
            return NotFound();

        return Ok(data);
    }

    // =============================
    // CREATE
    // =============================
    [HttpPost]
    public async Task<IActionResult> Create(SalesOrder order)
    {
        _context.SalesOrders.Add(order);
        await _context.SaveChangesAsync();

        return Ok(order);
    }

    // =============================
    // UPDATE
    // =============================
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, SalesOrder order)
    {
        if (id != order.OrderId)
            return BadRequest();

        _context.Entry(order).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return Ok(order);
    }

    // =============================
    // DELETE
    // =============================
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var data = await _context.SalesOrders.FindAsync(id);

        if (data == null)
            return NotFound();

        _context.SalesOrders.Remove(data);
        await _context.SaveChangesAsync();

        return Ok("Deleted Successfully");
    }

    // =============================
    // DOWNLOAD EXCEL TEMPLATE
    // =============================
    [HttpGet("download-template")]
    public IActionResult DownloadTemplate()
    {
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("SalesOrders");

        sheet.Cells[1, 1].Value = "CustomerId";
        sheet.Cells[1, 2].Value = "ScootyId";
        sheet.Cells[1, 3].Value = "OrderDate (yyyy-MM-dd)";
        sheet.Cells[1, 4].Value = "Quantity";
        sheet.Cells[1, 5].Value = "TotalAmount";

        var stream = new MemoryStream(package.GetAsByteArray());

        return File(stream,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "SalesOrderTemplate.xlsx");
    }

    // =============================
    // IMPORT EXCEL
    // =============================
    [HttpPost("import")]
    public async Task<IActionResult> ImportExcel(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var ordersToInsert = new List<SalesOrder>();
        int updatedCount = 0;
        var skippedRows = new List<int>();

        var existingOrders = await _context.SalesOrders.ToListAsync();

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        stream.Position = 0;

        using var package = new ExcelPackage(stream);
        var sheet = package.Workbook.Worksheets.FirstOrDefault();

        if (sheet == null)
            return BadRequest("Invalid Excel file.");

        int rowCount = sheet.Dimension.Rows;

        for (int row = 2; row <= rowCount; row++)
        {
            var customerText = sheet.Cells[row, 1].Text;
            var scootyText = sheet.Cells[row, 2].Text;
            var dateText = sheet.Cells[row, 3].Text;
            var qtyText = sheet.Cells[row, 4].Text;
            var amountText = sheet.Cells[row, 5].Text;

            if (!int.TryParse(customerText, out int customerId) ||
                !int.TryParse(scootyText, out int scootyId))
            {
                skippedRows.Add(row);
                continue;
            }

            DateOnly? orderDate = null;
            if (DateOnly.TryParse(dateText, out var d))
                orderDate = d;

            int quantity = int.TryParse(qtyText, out int q) ? q : 0;
            decimal? totalAmount = decimal.TryParse(amountText, out decimal a) ? a : null;

            // Check existing (based on Customer + Scooty + Date)
            var existing = existingOrders.FirstOrDefault(x =>
                x.CustomerId == customerId &&
                x.ScootyId == scootyId &&
                x.OrderDate == orderDate);

            if (existing != null)
            {
                existing.Quantity = quantity;
                existing.TotalAmount = totalAmount;
                updatedCount++;
                continue;
            }

            var order = new SalesOrder
            {
                CustomerId = customerId,
                ScootyId = scootyId,
                OrderDate = orderDate,
                Quantity = quantity,
                TotalAmount = totalAmount
            };

            ordersToInsert.Add(order);
        }

        if (ordersToInsert.Any())
            await _context.SalesOrders.AddRangeAsync(ordersToInsert);

        if (ordersToInsert.Any() || updatedCount > 0)
            await _context.SaveChangesAsync();

        var message = $"{ordersToInsert.Count} inserted, {updatedCount} updated.";

        if (skippedRows.Any())
            message += $" Skipped rows: {string.Join(",", skippedRows)}";

        return Ok(message);
    }
}