namespace ErpBackend.Services;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? Role { get; }
    int? BranchId { get; }
    bool IsAdmin { get; }
}
