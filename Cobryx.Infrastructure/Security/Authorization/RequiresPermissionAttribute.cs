using Microsoft.AspNetCore.Authorization;

namespace Cobryx.Infrastructure.Security.Authorization;

public class RequiresPermissionAttribute : AuthorizeAttribute
{
    public string Permission { get; }

    public RequiresPermissionAttribute(string permission) : base(permission)
    {
        Permission = permission;
    }
}
