namespace ErpBackend.Entities;

public class Group
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Kế toán kho"
    public int? BranchId { get; set; } // Optional branch association

    public List<GroupPermission> GroupPermissions { get; set; } = new();
    public List<UserGroup> UserGroups { get; set; } = new();
}
