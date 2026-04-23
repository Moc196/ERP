using System.Security.Claims;

namespace ErpBackend.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId 
    {
        get
        {
            var id = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(id, out var userId) ? userId : null;
        }
    }

    public string? Role => _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.Role);

    public int? BranchId
    {
        get
        {
            var branchId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("BranchId");
            return int.TryParse(branchId, out var id) ? id : null;
        }
    }

    public bool IsAdmin => Role == "Admin" || _httpContextAccessor.HttpContext == null;
}
