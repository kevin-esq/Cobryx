using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.ML;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

[Authorize]
public class TimeTravelController(
    ICobryxDbContext db, 
    ReplayEngine replayEngine, 
    ISender sender) : CobryxBaseController(sender)
{
    [HttpPost("/api/v1/risk/replay/{id}")]
    public async Task<IActionResult> Replay(Guid id)
    {
        var snapshot = await db.ReplaySnapshots.FindAsync(id);
        if (snapshot == null) return NotFound();

        var result = await replayEngine.ReplayAsync(snapshot);

        // Optionally, one could save the diffs back to DB or an audit log.

        return Ok(result);
    }
}
