using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ErpBackend.Data;
using ErpBackend.Dtos;
using ErpBackend.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ErpBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _context.Users
            .Include(u => u.UserGroups)
                .ThenInclude(ug => ug.Group!)
                    .ThenInclude(g => g.GroupPermissions)
                        .ThenInclude(gp => gp.Permission!)
            .SingleOrDefaultAsync(u => u.Username == dto.Username && u.Password == dto.Password);
        
        if (user == null)
            return Unauthorized(new { error = "Sai tài khoản hoặc mật khẩu!" });

        // Lấy danh sách các quyền từ các nhóm mà user tham gia
        var permissions = user.UserGroups
            .SelectMany(ug => ug.Group!.GroupPermissions)
            .Select(gp => gp.Permission!.Name)
            .Distinct()
            .ToList();

        var token = GenerateJwtToken(user, permissions);
        var branchName = user.BranchId.HasValue 
            ? (await _context.Branches.FindAsync(user.BranchId.Value))?.Name ?? "Chi nhánh"
            : "Tổng công ty";

        return Ok(new { 
            token, 
            role = user.Role, 
            branchId = user.BranchId ?? 0,
            branchName = branchName
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
            return BadRequest(new { error = "Tài khoản đã tồn tại!" });

        var user = new User
        {
            Username = dto.Username,
            Password = dto.Password, // Demo ERP code: no hash
            Role = dto.Role,
            BranchId = dto.Role == "Sales" ? 1 : null // Mặc định cho demo
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Tạo tài khoản thành công!" });
    }

    private string GenerateJwtToken(User user, List<string> permissions)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Secret not found.");
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.BranchId.HasValue)
        {
            claims.Add(new Claim("branchId", user.BranchId.Value.ToString()));
        }

        // Add permission claims
        foreach (var p in permissions)
        {
            claims.Add(new Claim("Permission", p));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
