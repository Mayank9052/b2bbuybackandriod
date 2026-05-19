using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using B2BBuyback.Api.Data;
using B2BBuyback.Api.Models;

namespace B2BBuyback.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class B2BCustomerController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public B2BCustomerController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // =============================
        // GET ALL
        // =============================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _context.B2bcustomers.ToListAsync();
            return Ok(customers);
        }

        // =============================
        // GET BY ID
        // =============================
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var customer = await _context.B2bcustomers.FindAsync(id);

            if (customer == null)
                return NotFound();

            return Ok(customer);
        }

        // =============================
        // CREATE CUSTOMER (ALL PARAM + LOGO)
        // =============================
        [HttpPost("create")]
        public async Task<IActionResult> Create(
            string companyName,
            string? contactPerson,
            string? address,
            string? email,
            string? phone,
            string? gstnumber,
            IFormFile? logo)
        {
            try
            {
                string? logoPath = null;

                if (logo != null && logo.Length > 0)
                {
                    var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                    string folderPath = Path.Combine(rootPath, "B2BCustomerLogo");

                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);

                    string fileName = Guid.NewGuid() + Path.GetExtension(logo.FileName);

                    string filePath = Path.Combine(folderPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await logo.CopyToAsync(stream);
                    }

                    logoPath = "/B2BCustomerLogo/" + fileName;
                }

                var customer = new B2bcustomer
                {
                    CompanyName = companyName,
                    ContactPerson = contactPerson,
                    Address = address,
                    Email = email,
                    Phone = phone,
                    Gstnumber = gstnumber,
                    LogoPath = logoPath
                };

                _context.B2bcustomers.Add(customer);
                await _context.SaveChangesAsync();

                return Ok(customer);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // =============================
        // UPDATE
        // =============================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(
            int id,
            string companyName,
            string? contactPerson,
            string? address,
            string? email,
            string? phone,
            string? gstnumber,
            IFormFile? logo)
        {
            var existing = await _context.B2bcustomers.FindAsync(id);

            if (existing == null)
                return NotFound();

            existing.CompanyName = companyName;
            existing.ContactPerson = contactPerson;
            existing.Address = address;
            existing.Email = email;
            existing.Phone = phone;
            existing.Gstnumber = gstnumber;

            if (logo != null && logo.Length > 0)
            {
                var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

                string folderPath = Path.Combine(rootPath, "B2BCustomerLogo");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName = Guid.NewGuid() + Path.GetExtension(logo.FileName);

                string filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logo.CopyToAsync(stream);
                }

                existing.LogoPath = "/B2BCustomerLogo/" + fileName;
            }

            await _context.SaveChangesAsync();

            return Ok(existing);
        }

        // =============================
        // DELETE
        // =============================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.B2bcustomers.FindAsync(id);

            if (customer == null)
                return NotFound();

            _context.B2bcustomers.Remove(customer);
            await _context.SaveChangesAsync();

            return Ok("Customer deleted successfully");
        }

        // =============================
        // DOWNLOAD BLANK EXCEL
        // =============================
        [HttpGet("download-template")]
        public IActionResult DownloadTemplate()
        {
            using var package = new ExcelPackage();

            var sheet = package.Workbook.Worksheets.Add("B2BCustomers");

            sheet.Cells[1, 1].Value = "CompanyName";
            sheet.Cells[1, 2].Value = "ContactPerson";
            sheet.Cells[1, 3].Value = "Address";
            sheet.Cells[1, 4].Value = "Email";
            sheet.Cells[1, 5].Value = "Phone";
            sheet.Cells[1, 6].Value = "GSTNumber";

            var stream = new MemoryStream(package.GetAsByteArray());

            return File(stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "B2BCustomerTemplate.xlsx");
        }

        // =============================
        // IMPORT EXCEL
        // =============================
        [HttpPost("import")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var customersToInsert = new List<B2bcustomer>();
            int updatedCount = 0;
            var skippedRows = new List<int>();

            var existingCustomers = await _context.B2bcustomers.ToListAsync();

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
                var companyName = sheet.Cells[row, 1].Text;
                var contactPerson = sheet.Cells[row, 2].Text;
                var address = sheet.Cells[row, 3].Text;
                var email = sheet.Cells[row, 4].Text;
                var phone = sheet.Cells[row, 5].Text;
                var gstNumber = sheet.Cells[row, 6].Text;

                if (string.IsNullOrWhiteSpace(companyName))
                {
                    skippedRows.Add(row);
                    continue;
                }

                var existing = existingCustomers
                    .FirstOrDefault(x => x.Email == email || x.Gstnumber == gstNumber);

                if (existing != null)
                {
                    // UPDATE
                    existing.CompanyName = companyName;
                    existing.ContactPerson = contactPerson;
                    existing.Address = address;
                    existing.Phone = phone;

                    updatedCount++;
                    continue;
                }

                // INSERT
                var customer = new B2bcustomer
                {
                    CompanyName = companyName,
                    ContactPerson = contactPerson,
                    Address = address,
                    Email = email,
                    Phone = phone,
                    Gstnumber = gstNumber
                };

                customersToInsert.Add(customer);
            }

            if (customersToInsert.Any())
                await _context.B2bcustomers.AddRangeAsync(customersToInsert);

            if (customersToInsert.Any() || updatedCount > 0)
                await _context.SaveChangesAsync();

            var message = $"{customersToInsert.Count} inserted, {updatedCount} updated.";

            if (skippedRows.Any())
                message += $" Skipped rows: {string.Join(",", skippedRows)}";

            return Ok(message);
        }
    }
}