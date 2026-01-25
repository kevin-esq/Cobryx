using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public class Permission : BaseEntity, IAggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }

    // Navigation
    private readonly List<Role> _roles = new();
    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();

    private Permission() { }

    public Permission(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public static class Constants
    {
        public const string ViewCustomers = "customers:view";
        public const string CreateCustomers = "customers:create";
        public const string ViewCredits = "credits:view";
        public const string CreateCredits = "credits:create";
        public const string ApplyPayments = "payments:apply";
        public const string ManageTenant = "tenant:manage";
        public const string ManageUsers = "users:manage";
    }
}
