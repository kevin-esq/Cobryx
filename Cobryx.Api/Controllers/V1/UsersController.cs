using Asp.Versioning;

using Cobryx.Application.Users.Commands.UpdateUserRole;
using Cobryx.Application.Users.Queries.GetAvailableRoles;
using Cobryx.Application.Users.Queries.GetTenantUsers;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/users")]
[Authorize(Policy = "CanManageTenant")]
public class UsersController : CobryxBaseController
{
    public UsersController(IMediator mediator) : base(mediator)
    {
    }

    /// <summary>
    /// List all users in the tenant (Paged).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await Sender.Send(new GetTenantUsersQuery(page, pageSize));
        return Ok(result);
    }

    /// <summary>
    /// List assignable roles.
    /// </summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var result = await Sender.Send(new GetAvailableRolesQuery());
        return Ok(result);
    }

    /// <summary>
    /// Update a user's role.
    /// </summary>
    [HttpPatch("{id}/role")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request)
    {
        var result = await Sender.Send(new UpdateUserRoleCommand(id, request.RoleId));

        if (result.IsFailure)
        {
            return BadRequest(result.Error);
        }

        return NoContent();
    }
}

public record UpdateRoleRequest(Guid RoleId);
