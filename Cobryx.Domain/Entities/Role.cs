using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public class Role : BaseEntity, IAggregateRoot
{
    public static class Constants
    {
        public const string Owner = "Owner";
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Accountant = "Accountant";
        public const string User = "User";
    }

    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsSystemRole { get; private set; }

    private readonly List<Permission> _permissions = new();
    public IReadOnlyCollection<Permission> Permissions => _permissions.AsReadOnly();

    private Role()
    {
        Name = null!;
        Description = null!;
    }

    public Role(string name, string description, bool isSystemRole = false)
    {
        Name = name;
        Description = description;
        IsSystemRole = isSystemRole;
    }

    public void AddPermission(Permission permission)
    {
        if (!_permissions.Any(p => p.Name == permission.Name))
        {
            _permissions.Add(permission);
        }
    }

    public void RemovePermission(string permissionName)
    {
        var permission = _permissions.FirstOrDefault(p => p.Name == permissionName);
        if (permission != null)
        {
            _permissions.Remove(permission);
        }
    }
}
