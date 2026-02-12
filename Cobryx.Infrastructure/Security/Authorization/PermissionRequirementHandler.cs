using System.Security.Claims;
using Cobryx.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace Cobryx.Infrastructure.Security.Authorization;

public class PermissionRequirementHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;

    public PermissionRequirementHandler(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, 
        PermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return;
        }

        if (await _permissionService.HasPermissionAsync(userId, requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
