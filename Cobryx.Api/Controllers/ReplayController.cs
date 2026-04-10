using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Application.ML.Commands.ExecuteReplay;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

/// <summary>
/// Provides audit replay capabilities for ML decision snapshots.
/// Used for determinism verification and drift detection.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ml/replay")]
[Authorize(Roles = "Admin,Audit")]
[Tags("ML & Risk")]
public class ReplayController(ISender sender) : CobryxBaseController(sender)
{
    /// <summary>
    /// Executes a replay of a historical ML decision snapshot to verify determinism.
    /// </summary>
    /// <param name="snapshotId">The unique identifier of the snapshot to replay.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// This endpoint is used for audit and compliance purposes to verify that
    /// ML decisions can be reproduced deterministically.
    ///
    /// Possible Outcomes:
    /// - ML.REPLAY.COMPLETED: Replay executed successfully with deterministic results.
    /// - ML.REPLAY.DRIFT_DETECTED: Replay completed but output differs from original.
    /// </remarks>
    /// <response code="200">Replay result with determinism status.</response>
    /// <response code="404">Snapshot not found.</response>
    [HttpPost("{snapshotId:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<ReplayResultDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> Replay(Guid snapshotId, CancellationToken ct)
    {
        Result<ReplayResultDto> result = await Sender.Send(new ExecuteReplayCommand(snapshotId), ct);

        Outcome outcome = result.IsSuccess && !result.Value!.IsDeterministic
            ? MlOutcomes.Replay.DriftDetected
            : MlOutcomes.Replay.Completed;

        return HandleResult(result, outcome);
    }
}
