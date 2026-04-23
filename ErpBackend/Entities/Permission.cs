namespace ErpBackend.Entities;

public class Permission
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "product.view"
    public string Description { get; set; } = string.Empty;

    public List<GroupPermission> GroupPermissions { get; set; } = new();
}
