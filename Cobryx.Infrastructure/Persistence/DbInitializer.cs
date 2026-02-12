using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Persistence;

public class DbInitializer
{
    public static async Task SeedRolesAsync(IRoleRepository roleRepository, IUnitOfWork unitOfWork)
    {
        try
        {
            var permissions = new[]
            {
                new Permission(Permission.Constants.CustomersView, "Allow viewing customers"),
                new Permission(Permission.Constants.CustomersCreate, "Allow creating customers"),
                new Permission(Permission.Constants.CustomersEdit, "Allow editing customers"),
                new Permission(Permission.Constants.CustomersDelete, "Allow deleting customers"),
                new Permission(Permission.Constants.InvoicesView, "Allow viewing invoices"),
                new Permission(Permission.Constants.InvoicesCreate, "Allow creating invoices"),
                new Permission(Permission.Constants.InvoicesCancel, "Allow cancelling invoices"),
                new Permission(Permission.Constants.PaymentsView, "Allow viewing payments"),
                new Permission(Permission.Constants.PaymentsApply, "Allow applying payments"),
                new Permission(Permission.Constants.LoansView, "Allow viewing loans"),
                new Permission(Permission.Constants.LoansCreate, "Allow creating loans"),
                new Permission(Permission.Constants.TenantManage, "Allow managing business settings"),
                new Permission(Permission.Constants.UsersManage, "Allow managing tenant users"),
                new Permission(Permission.Constants.DashboardView, "Allow viewing dashboard"),
                new Permission(Permission.Constants.SubscriptionView, "Allow viewing subscription")
            };

            var roles = new[]
            {
                new Role("Owner", "Full access to the tenant and settings", true),
                new Role("Admin", "Full management of credits and customers", true),
                new Role("Manager", "Operations management without tenant settings", true),
                new Role("Accountant", "Financial oversight and reports", true)
            };

            bool anyAdded = false;
            foreach (var template in roles)
            {
                var role = await roleRepository.GetByNameAsync(template.Name);
                bool isNew = false;
                if (role == null)
                {
                    role = template;
                    isNew = true;
                }

                // Determine target permissions for this specific role
                IEnumerable<Permission> targetPermissions;
                if (role.Name == "Owner")
                {
                    targetPermissions = permissions;
                }
                else if (role.Name == "Admin")
                {
                    targetPermissions = permissions.Where(x => !x.Name.StartsWith("tenant."));
                }
                else if (role.Name == "Manager")
                {
                    var managerPerms = new[]
                    {
                        Permission.Constants.CustomersView, Permission.Constants.CustomersEdit,
                        Permission.Constants.InvoicesView, Permission.Constants.InvoicesCreate,
                        Permission.Constants.LoansView, Permission.Constants.DashboardView
                    };
                    targetPermissions = permissions.Where(x => managerPerms.Contains(x.Name));
                }
                else if (role.Name == "Accountant")
                {
                    var accountantPerms = new[]
                    {
                        Permission.Constants.InvoicesView, Permission.Constants.PaymentsView,
                        Permission.Constants.LoansView, Permission.Constants.DashboardView
                    };
                    targetPermissions = permissions.Where(x => accountantPerms.Contains(x.Name));
                }
                else
                {
                    targetPermissions = Enumerable.Empty<Permission>();
                }

                // Sync permissions
                foreach (var p in targetPermissions)
                {
                    if (!role.Permissions.Any(ep => ep.Name == p.Name))
                    {
                        role.AddPermission(p);
                        anyAdded = true;
                    }
                }

                if (isNew)
                {
                    await roleRepository.AddAsync(role);
                    anyAdded = true;
                }
            }

            if (anyAdded)
            {
                await unitOfWork.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Fatal error during SeedRolesAsync");
            throw;
        }
    }

    public static async Task SeedPlansAsync(CobryxDbContext dbContext)
    {
        if (await dbContext.SubscriptionPlans.AnyAsync()) return;

        var plans = new[]
        {
            new SubscriptionPlan("Basic", "Perfect for small lenders", new Money(999, "MXN"), 100, 3),
            new SubscriptionPlan("Professional", "Scale your lending business", new Money(2499, "MXN"), 1000, 10)
        };

        await dbContext.SubscriptionPlans.AddRangeAsync(plans);
        await dbContext.SaveChangesAsync();
    }
}
