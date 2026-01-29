using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;

namespace Cobryx.Infrastructure.Persistence;

public class DbInitializer
{
    public static async Task SeedRolesAsync(IRoleRepository roleRepository)
    {
        var roles = new[]
        {
            new Role("Owner", "Full access to the tenant and settings", true),
            new Role("Admin", "Full management of credits and customers", true),
            new Role("Collector", "Register payments and view customer status", true)
        };

        var permissions = new[]
        {
            new Permission(Permission.Constants.ViewCustomers, "Allow viewing customers"),
            new Permission(Permission.Constants.CreateCustomers, "Allow creating customers"),
            new Permission(Permission.Constants.ViewCredits, "Allow viewing credits"),
            new Permission(Permission.Constants.CreateCredits, "Allow creating credits"),
            new Permission(Permission.Constants.ApplyPayments, "Allow applying payments"),
            new Permission(Permission.Constants.ManageTenant, "Allow managing business settings"),
            new Permission(Permission.Constants.ManageUsers, "Allow managing tenant users")
        };

        foreach (var role in roles)
        {
            if (await roleRepository.GetByNameAsync(role.Name) == null)
            {

                if (role.Name == "Owner")
                    foreach (var p in permissions) role.AddPermission(p);

                if (role.Name == "Admin")
                    foreach (var p in permissions.Where(x => x.Name != Permission.Constants.ManageTenant)) role.AddPermission(p);

                if (role.Name == "Collector")
                {
                    role.AddPermission(permissions.First(x => x.Name == Permission.Constants.ViewCustomers));
                    role.AddPermission(permissions.First(x => x.Name == Permission.Constants.ApplyPayments));
                }

                await roleRepository.AddAsync(role);
            }
        }
    }
}
