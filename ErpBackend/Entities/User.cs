namespace ErpBackend.Entities;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public int? BranchId { get; set; } // For RLS

    public List<UserGroup> UserGroups { get; set; } = new();
}
