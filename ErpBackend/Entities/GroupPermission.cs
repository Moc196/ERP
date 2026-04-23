namespace ErpBackend.Entities;

public class GroupPermission
{
    public int GroupId { get; set; }
    public Group? Group { get; set; }

    public int PermissionId { get; set; }
    public Permission? Permission { get; set; }
}
