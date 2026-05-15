using Microsoft.AspNetCore.Authorization;

namespace Cobryx.Api.Attributes;

/// <summary>
/// Gated authorization based on granular permission strings.
/// Preferred over Role-based authorization for sensitive financial and system operations.
/// </summary>
public class AuthorizePermissionAttribute : AuthorizeAttribute
{
    public string Permission { get; }

    public AuthorizePermissionAttribute(string permission)
    {
        Permission = permission;
        Policy = permission;
    }
}
