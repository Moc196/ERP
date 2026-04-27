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
            var query = _context.Customers
                .Include(c => c.CustomerBranches)
                    .ThenInclude(cb => cb.Branch)
                .OrderByDescending(c => c.CreatedAt);

            return await query.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetCustomer(int id)
        {
            var customer = await _context.Customers
                .Include(c => c.CustomerBranches)
                    .ThenInclude(cb => cb.Branch)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (customer == null) return NotFound();
            return customer;
        }

        [HttpPost]
        public async Task<ActionResult<Customer>> CreateCustomer(Customer customer)
        {
            // Chuẩn hóa SĐT
            var phone = customer.Phone?.Trim();

            // Kiểm tra xem khách hàng đã tồn tại trong hệ thống chưa (bất kể chi nhánh nào) theo Số điện thoại
            var existingCustomer = await _context.Customers
                .IgnoreQueryFilters()
                .Include(c => c.CustomerBranches)
                .FirstOrDefaultAsync(c => !string.IsNullOrEmpty(phone) && c.Phone == phone);

            if (existingCustomer != null)
            {
                // Nếu đã tồn tại, kiểm tra xem đã có ở chi nhánh này chưa
                var targetBranchId = customer.BranchIds?.FirstOrDefault() ?? _currentUser.BranchId;

                if (targetBranchId.HasValue)
                {
                    var alreadyLinked = existingCustomer.CustomerBranches.Any(cb => cb.BranchId == targetBranchId.Value);
                    if (!alreadyLinked)
                    {
                        // Cập nhật thông tin nếu có thông tin mới hơn (tùy chọn)
                        if (!string.IsNullOrEmpty(customer.Name)) existingCustomer.Name = customer.Name;
                        if (!string.IsNullOrEmpty(customer.Address)) existingCustomer.Address = customer.Address;
                        if (!string.IsNullOrEmpty(customer.Email)) existingCustomer.Email = customer.Email;

                        _context.CustomerBranches.Add(new CustomerBranch 
                        { 
                            CustomerId = existingCustomer.Id, 
                            BranchId = targetBranchId.Value 
                        });
                        await _context.SaveChangesAsync();
                        
                        // Lấy lại dữ liệu đầy đủ để trả về
                        var updated = await _context.Customers
                            .Include(c => c.CustomerBranches)
                                .ThenInclude(cb => cb.Branch)
                            .FirstOrDefaultAsync(c => c.Id == existingCustomer.Id);

                        return Ok(updated);
                    }
                    else
                    {
                        // Nếu đã có rồi thì trả về luôn thay vì báo lỗi (để frontend đỡ báo đỏ)
                        return Ok(existingCustomer);
                    }
                }
            }

            customer.CreatedAt = DateTime.UtcNow;
            
            // Tự động sinh mã nếu trống
            if (string.IsNullOrEmpty(customer.CustomerCode))
            {
                var count = await _context.Customers.IgnoreQueryFilters().CountAsync();
                customer.CustomerCode = $"KH{DateTime.Now:yyyyMMdd}{count + 1:D3}";
            }
            else if (await _context.Customers.IgnoreQueryFilters().AnyAsync(c => c.CustomerCode == customer.CustomerCode))
            {
                return BadRequest(new { error = "Mã khách hàng này đã tồn tại!" });
            }

            // Xác định chi nhánh trước khi lưu
            var branchIdsToAssign = new List<int>();
            if (customer.BranchIds != null && customer.BranchIds.Any())
            {
                branchIdsToAssign = customer.BranchIds;
            }
            else if (_currentUser.BranchId.HasValue)
            {
                branchIdsToAssign.Add(_currentUser.BranchId.Value);
            }

            foreach (var bId in branchIdsToAssign)
            {
                customer.CustomerBranches.Add(new CustomerBranch { BranchId = bId });
            }

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, Customer customer)
        {
            if (id != customer.Id) return BadRequest();

            var existing = await _context.Customers
                .Include(c => c.CustomerBranches)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (existing == null) return NotFound();

            // Cập nhật thông tin cơ bản
            _context.Entry(existing).CurrentValues.SetValues(customer);

            // Cập nhật chi nhánh nếu có gửi lên (thường là Admin)
            if (customer.BranchIds != null)
            {
                existing.CustomerBranches.Clear();
                foreach (var bId in customer.BranchIds)
                {
                    existing.CustomerBranches.Add(new CustomerBranch { BranchId = bId });
                }
            }

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
            var existingCustomersToLink = new List<Customer>();
            var skipCount = 0;
            var rowIdx = 1;

            foreach (var row in rows)
            {
                rowIdx++;
                var name = row.Cell(1).GetValue<string>();
                var phone = row.Cell(2).GetValue<string>();
                
                if (string.IsNullOrEmpty(name)) continue;

                // Kiểm tra xem khách hàng đã tồn tại chưa (IgnoreQueryFilters để tìm xuyên chi nhánh)
                var existing = await _context.Customers
                    .IgnoreQueryFilters()
                    .Include(c => c.CustomerBranches)
                    .FirstOrDefaultAsync(c => !string.IsNullOrEmpty(phone) && c.Phone == phone);

                if (existing != null)
                {
                    existingCustomersToLink.Add(existing);
                    continue;
                }

                // Kiểm tra trùng trong danh sách đang xử lý
                if (newCustomers.Any(nc => !string.IsNullOrEmpty(phone) && nc.Phone == phone))
                {
                    skipCount++;
                    continue;
                }

                var customer = new Customer
                {
                    Name = name,
                    Phone = phone,
                    Email = row.Cell(3).GetValue<string>(),
                    Address = row.Cell(4).GetValue<string>(),
                    TaxId = row.Cell(5).GetValue<string>(),
                    CreatedAt = DateTime.UtcNow
                };

                // Tự động sinh mã
                var count = (await _context.Customers.IgnoreQueryFilters().CountAsync()) + newCustomers.Count;
                customer.CustomerCode = $"KH{DateTime.Now:yyyyMMdd}{count + 1:D3}";

                newCustomers.Add(customer);
            }

            if (newCustomers.Any() || existingCustomersToLink.Any())
            {
                if (newCustomers.Any())
                {
                    _context.Customers.AddRange(newCustomers);
                    await _context.SaveChangesAsync();
                }

                // Liên kết với chi nhánh
                var branchIds = new List<int>();
                if (_currentUser.BranchId.HasValue)
                {
                    branchIds.Add(_currentUser.BranchId.Value);
                }
                else if (_currentUser.IsAdmin)
                {
                    branchIds = await _context.Branches.Select(b => b.Id).ToListAsync();
                }

                if (branchIds.Any())
                {
                    // Xử lý khách hàng mới
                    foreach (var nc in newCustomers)
                    {
                        foreach (var bId in branchIds)
                        {
                            _context.CustomerBranches.Add(new CustomerBranch { CustomerId = nc.Id, BranchId = bId });
                        }
                    }

                    // Xử lý khách hàng cũ (chỉ link nếu chưa có)
                    foreach (var ec in existingCustomersToLink)
                    {
                        foreach (var bId in branchIds)
                        {
                            var alreadyLinked = ec.CustomerBranches.Any(cb => cb.BranchId == bId);
                            if (!alreadyLinked)
                            {
                                _context.CustomerBranches.Add(new CustomerBranch { CustomerId = ec.Id, BranchId = bId });
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }

            return Ok(new { count = newCustomers.Count, linked = existingCustomersToLink.Count, skipped = skipCount });
        }

        private bool CustomerExists(int id)
        {
            return _context.Customers.Any(e => e.Id == id);
        }
    }
}
