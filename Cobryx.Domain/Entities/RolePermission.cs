namespace Cobryx.Domain.Entities;

public class RolePermission
{
    public Guid RoleId { get; private set; }
    public string PermissionKey { get; private set; } = null!;

    public virtual Role Role { get; private set; } = null!;
    public virtual Permission Permission { get; private set; } = null!;

    private RolePermission() { }

    public RolePermission(Guid roleId, string permissionKey)
    {
        RoleId = roleId;
        PermissionKey = permissionKey;
    }
}
