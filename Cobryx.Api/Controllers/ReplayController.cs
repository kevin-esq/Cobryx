using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Decision.Models;
using Cobryx.Application.ML;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Api.Controllers;

[ApiController]
[Route("api/ml/[controller]")]
[Authorize(Roles = "Admin,Audit")]
public class ReplayController(ReplayEngine replayEngine, ICobryxDbContext db, ILogger<ReplayController> logger)
    : ControllerBase
{
    [HttpPost("{snapshotId:guid}")]
    public async Task<IActionResult> Replay(Guid snapshotId)
    {
        logger.LogInformation("Audit Replay requested for Snapshot {SnapshotId} by {User}", snapshotId,
            User.Identity?.Name);

        var snapshot = await db.ReplaySnapshots
            .FirstOrDefaultAsync(x => x.Id == snapshotId);

        if (snapshot == null)
        {
            logger.LogWarning("Replay failed: Snapshot {SnapshotId} not found", snapshotId);
            return NotFound();
        }

        try
        {
            var adapter = new ReplaySnapshotAdapter(snapshot);
            var result = await replayEngine.ReplayAsync(adapter);

            if (!result.IsDeterministic)
            {
                logger.LogCritical("DETERMINISM DRIFT DETECTED: Snapshot {SnapshotId} failed verification.",
                    snapshotId);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Replay execution failed for Snapshot {SnapshotId}", snapshotId);
            return StatusCode(500, "Internal error during replay execution");
        }
    }
}
