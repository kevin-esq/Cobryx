using Asp.Versioning;

using Cobryx.Application.Tenants.Commands.InviteUser;
using Cobryx.Application.Tenants.Commands.RevokeInvitation;
using Cobryx.Application.Tenants.Common;
using Cobryx.Application.Tenants.Queries.GetPendingInvitations;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/invitations")]
[Tags("Platform")]
public class InvitationsController(ISender sender) : CobryxBaseController(sender)
{

    /// <summary>
    /// Invites a new user to the current tenant.
    /// </summary>
    /// <param name="request">Invitation details (email and role).</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpPost]
    [Authorize(Policy = "CanManageTenant")]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 402)]
    public async Task<IActionResult> Invite([FromBody] InviteUserRequest request, CancellationToken ct)
    {
        Result<Guid> result = await Sender.Send(new InviteUserCommand(request.Email, request.RoleName), ct);
        return HandleResult(result, InvitationOutcomes.InviteSuccess);
    }

    /// <summary>
    /// Lists all pending invitations for the current tenant.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    [HttpGet]
    [Authorize(Policy = "CanManageTenant")]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<InvitationDto>>), 200)]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        Result<List<InvitationDto>> result = await Sender.Send(new GetPendingInvitationsQuery(), ct);
        return HandleResult(result, InvitationOutcomes.FetchSuccess);
    }

    /// <summary>
    /// Revokes an existing pending invitation.
    /// </summary>
    /// <param name="id">Invitation ID.</param>
    /// <param name="ct">Cancellation token.</param>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CanManageTenant")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        Result result = await Sender.Send(new RevokeInvitationCommand(id), ct);
        return HandleResult(result, InvitationOutcomes.RevokeSuccess);
    }
}
