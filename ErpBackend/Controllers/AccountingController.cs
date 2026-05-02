using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ErpBackend.Data;
using ErpBackend.Entities;
using Microsoft.EntityFrameworkCore;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountingController : ControllerBase
{
    private readonly AppDbContext _context;

    public AccountingController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("trial-balance")]
    public async Task<IActionResult> GetTrialBalance([FromServices] Services.ICurrentUserService currentUserService)
    {
        var branchId = currentUserService.BranchId;
        bool isAdmin = currentUserService.IsAdmin;

        // Tính toán số dư cho từng tài khoản dựa trên các dòng bút toán
        // Lọc theo chi nhánh nếu không phải Admin
        var accounts = await _context.Accounts
            .Include(a => a.AccountType)
            .Select(a => new
            {
                a.Id,
                a.Code,
                a.Name,
                TypeName = a.AccountType.Name,
                NormalBalance = a.AccountType.NormalBalance,
                // Chỉ Sum những dòng thuộc JournalEntry của chi nhánh tương ứng
                TotalDebit = a.JournalEntryLines
                    .Where(l => isAdmin || l.JournalEntry.BranchId == branchId)
                    .Sum(l => l.Debit),
                TotalCredit = a.JournalEntryLines
                    .Where(l => isAdmin || l.JournalEntry.BranchId == branchId)
                    .Sum(l => l.Credit)
            })
            .ToListAsync();

        var result = accounts.Select(a => new
        {
            a.Id,
            a.Code,
            a.Name,
            a.TypeName,
            a.TotalDebit,
            a.TotalCredit,
            Balance = a.NormalBalance == "Debit" 
                ? a.TotalDebit - a.TotalCredit 
                : a.TotalCredit - a.TotalDebit
        }).OrderBy(a => a.Code);

        return Ok(result);
    }

    [HttpGet("journal-entries")]
    public async Task<IActionResult> GetJournalEntries([FromQuery] int limit = 50)
    {
        var entries = await _context.JournalEntries
            .Include(e => e.Lines)
                .ThenInclude(l => l.Account)
            .OrderByDescending(e => e.EntryDate)
            .Take(limit)
            .Select(e => new
            {
                e.Id,
                e.EntryDate,
                e.Reference,
                e.Description,
                Lines = e.Lines.Select(l => new
                {
                    l.Account.Code,
                    l.Account.Name,
                    l.Debit,
                    l.Credit
                })
            })
            .ToListAsync();

        return Ok(entries);
    }
}
