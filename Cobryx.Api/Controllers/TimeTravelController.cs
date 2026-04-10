using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Application.ML.Commands.ExecuteReplay;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

/// <summary>
/// Provides time-travel replay capabilities for risk analysis.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/risk")]
[Authorize]
[Tags("ML & Risk")]
public class TimeTravelController(ISender sender) : CobryxBaseController(sender)
{
    /// <summary>
    /// Replays a historical snapshot for risk analysis.
    /// </summary>
    /// <param name="id">The snapshot identifier.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - ML.REPLAY.COMPLETED: Replay executed successfully.
    /// - ML.REPLAY.DRIFT_DETECTED: Replay completed with drift.
    /// </remarks>
    [HttpPost("replay/{id:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<ReplayResultDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> Replay(Guid id, CancellationToken ct)
    {
        Result<ReplayResultDto> result = await Sender.Send(new ExecuteReplayCommand(id), ct);
        return HandleResult(result, MlOutcomes.Replay.Completed);
    }
}
