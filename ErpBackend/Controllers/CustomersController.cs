using ErpBackend.Data;
using ErpBackend.Entities;
using ErpBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace ErpBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICurrentUserService _currentUser;

        public CustomersController(AppDbContext context, ICurrentUserService currentUser)
        {
            _context = context;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            return await _context.Customers.OrderByDescending(c => c.CreatedAt).ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();
            return customer;
        }

        [HttpPost]
        public async Task<ActionResult<Customer>> CreateCustomer(Customer customer)
        {
            customer.BranchId = _currentUser.BranchId;
            customer.CreatedAt = DateTime.UtcNow;

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, Customer customer)
        {
            if (id != customer.Id) return BadRequest();

            _context.Entry(customer).State = EntityState.Modified;
            customer.BranchId = _currentUser.BranchId; // Ensure branch doesn't change

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CustomerExists(id)) return NotFound();
                throw;
            }

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null) return NotFound();

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportExcel()
        {
            var customers = await _context.Customers.ToListAsync();
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Customers");
            
            // Header
            worksheet.Cell(1, 1).Value = "Tên";
            worksheet.Cell(1, 2).Value = "SĐT";
            worksheet.Cell(1, 3).Value = "Email";
            worksheet.Cell(1, 4).Value = "Địa chỉ";
            worksheet.Cell(1, 5).Value = "Mã số thuế";
            
            var range = worksheet.Range(1, 1, 1, 5);
            range.Style.Font.Bold = true;
            range.Style.Fill.BackgroundColor = XLColor.LightBlue;

            for (int i = 0; i < customers.Count; i++)
            {
                worksheet.Cell(i + 2, 1).Value = customers[i].Name;
                worksheet.Cell(i + 2, 2).Value = customers[i].Phone;
                worksheet.Cell(i + 2, 3).Value = customers[i].Email;
                worksheet.Cell(i + 2, 4).Value = customers[i].Address;
                worksheet.Cell(i + 2, 5).Value = customers[i].TaxId;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            var content = stream.ToArray();

            return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Customers.xlsx");
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest("Vui lòng chọn file Excel");

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            using var workbook = new XLWorkbook(stream);
            var worksheet = workbook.Worksheet(1);
            var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // Bỏ qua header

            var newCustomers = new List<Customer>();
            foreach (var row in rows)
            {
                newCustomers.Add(new Customer
                {
                    Name = row.Cell(1).GetValue<string>(),
                    Phone = row.Cell(2).GetValue<string>(),
                    Email = row.Cell(3).GetValue<string>(),
                    Address = row.Cell(4).GetValue<string>(),
                    TaxId = row.Cell(5).GetValue<string>(),
                    BranchId = _currentUser.BranchId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.Customers.AddRange(newCustomers);
            await _context.SaveChangesAsync();

            return Ok(new { count = newCustomers.Count });
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.Id == id);
        }
    }
}
