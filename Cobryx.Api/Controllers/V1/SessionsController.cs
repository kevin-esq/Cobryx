using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Application.Auth.Commands.Sessions;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Controller for managing authenticated user sessions, device tracking, and remote session revocation.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sessions")]
[Tags("Platform")]
public class SessionsController(ISender sender) : CobryxBaseController(sender)
{

    /// <summary>
    /// Retrieves a list of all active sessions across different devices for the authenticated user.
    /// </summary>
    /// <remarks>
    /// Allows users to audit where their account is currently logged in.
    ///
    /// Possible Outcomes:
    /// - AUTH.SESSION.SEARCH.COMPLETED: Sessions successfully retrieved.
    /// </remarks>
    /// <response code="200">A collection of active session details.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<SessionContract>>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    public async Task<IActionResult> GetSessions()
    {
        Result<List<SessionResponse>> result = await Sender.Send(new GetSessionsQuery());

        if (!result.IsSuccess)
        {
            return HandleResult(result, AuthOutcomes.SessionSearchCompleted);
        }

        var mappedResult = result.Value?.Select(s => new SessionContract(
            s.Id,
            s.IpAddress,
            s.DeviceFingerprint,
            s.DeviceName,
            s.LastActiveAt,
            s.IsCurrent)).ToList();

        return Success(mappedResult, AuthOutcomes.SessionSearchCompleted);
    }

    /// <summary>
    /// Formally revokes and terminates a specific user session by its identifier.
    /// </summary>
    /// <param name="id">Unique identifier of the session to terminate.</param>
    /// <remarks>
    /// Once revoked, the associated device will be forced to re-authenticate.
    ///
    /// Possible Outcomes:
    /// - AUTH.SESSION.REVOKED: Session successfully terminated.
    /// - AUTH.SESSION.FAILED: Session not found or unauthorized for the current user context.
    /// </remarks>
    /// <response code="204">Session successfully revoked.</response>
    /// <response code="404">Session not found or already expired.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> RevokeSession(Guid id)
    {
        Result<bool> result = await Sender.Send(new RevokeSessionCommand(id));
        return HandleDeleteResult(result, AuthOutcomes.SessionRevoked);
    }
}
