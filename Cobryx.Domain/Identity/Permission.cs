using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Identity;

public class Permission : BaseEntity, IAggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }


    private readonly List<Role> _roles = new();
    public IReadOnlyCollection<Role> Roles => _roles.AsReadOnly();

    private Permission()
    {
        Name = null!;
        Description = null!;
    }

    public Permission(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public static class Constants
    {
        public const string CustomersView = "customers.view";
        public const string CustomersCreate = "customers.create";
        public const string CustomersEdit = "customers.edit";
        public const string CustomersDelete = "customers.delete";

        public const string InvoicesView = "invoices.view";
        public const string InvoicesCreate = "invoices.create";
        public const string InvoicesCancel = "invoices.cancel";
        public const string InvoicesExport = "invoices.export";

        public const string PaymentsView = "payments.view";
        public const string PaymentsApply = "payments.apply";
        public const string PaymentsRefund = "payments.refund";

        public const string LoansView = "loans.view";
        public const string LoansCreate = "loans.create";
        public const string LoansDisburse = "loans.disburse";
        public const string LoansManagePolicies = "loans.policies.manage";

        public const string TenantManage = "tenant.manage";
        public const string TenantViewSettings = "tenant.settings.view";
        public const string UsersView = "users.view";
        public const string UsersManage = "users.manage";

        public const string AuditView = "audit.view";
        public const string DashboardView = "dashboard.view";
        public const string SubscriptionView = "subscription.view";
        public const string SubscriptionManage = "subscription.manage";
    }
}
