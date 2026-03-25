using System.Text.Json;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Decision;
using Cobryx.Application.Decision.Models;
using Cobryx.Application.ML;
using Cobryx.Application.ML.Models;
using Cobryx.Domain.Decision;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Api.Controllers;

[ApiController]
[Route("api/ml/[controller]")]
[Authorize(Roles = "Admin,Audit")]
public class RegressionController(
    SnapshotRegressionRunner runner,
    ReplayEngine replayEngine,
    ICobryxDbContext db,
    ILogger<RegressionController> logger) : ControllerBase
{
    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] RunRegressionRequest request)
    {
        logger.LogInformation("Regression run requested by {User}. SampleRate: {SampleRate}%",
            User.Identity?.Name, request.SampleRate);

        try
        {
            var profile = request.Profile ?? new DriftToleranceProfile();
            var suiteResult = await runner.RunRegressionAsync(
                profile,
                request.BaselineVersion,
                request.SampleRate,
                request.EarlyStopThreshold,
                HttpContext.RequestAborted);

            var dbReport = new RegressionReport(
                suiteResult.TotalProcessed,
                suiteResult.PassedCount,
                suiteResult.FailedCount,
                suiteResult.NonComparableCount,
                suiteResult.MeanLimitDrift,
                suiteResult.P95LimitDrift,
                suiteResult.MaxLimitDrift,
                EngineMetadata.EngineVersion,
                request.BaselineVersion,
                suiteResult.ParentEngineVersion,
                suiteResult.SampleRate,
                suiteResult.SampleSize,
                suiteResult.DatasetHash,
                JsonSerializer.Serialize(suiteResult.TopFailures),
                JsonSerializer.Serialize(suiteResult.SeverityDistribution)
            );

            db.RegressionReports.Add(dbReport);
            await db.SaveChangesAsync(HttpContext.RequestAborted);

            return Ok(new
            {
                suiteResult.Id,
                suiteResult.TotalProcessed,
                suiteResult.DriftRate,
                suiteResult.ComparableRatio,
                suiteResult.PassedCount,
                suiteResult.FailedCount,
                suiteResult.NonComparableCount,
                suiteResult.SeverityDistribution
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Regression run failed");
            return StatusCode(500, "Internal error during regression run");
        }
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetReports()
    {
        var reports = await db.RegressionReports
            .OrderByDescending(r => r.RunAt)
            .Take(20)
            .ToListAsync(HttpContext.RequestAborted);

        return Ok(reports);
    }

    [HttpGet("reports/{id:guid}")]
    public async Task<IActionResult> GetReport(Guid id)
    {
        var report = await db.RegressionReports
            .FirstOrDefaultAsync(r => r.Id == id, HttpContext.RequestAborted);

        if (report == null)
            return NotFound();

        return Ok(report);
    }

    [HttpGet("replay/{id}")]
    public async Task<IActionResult> DebugReplay(Guid id, [FromQuery] decimal limitThreshold = 10,
        [FromQuery] decimal rateThreshold = 0.01m)
    {
        var snapshot = await db.ProductionSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, HttpContext.RequestAborted);

        if (snapshot == null)
            return NotFound();

        var input = new ProductionSnapshotAdapter(snapshot);
        var replayResult = await replayEngine.ReplayAsync(input);

        var profile = new DriftToleranceProfile
        {
            CreditLimitAbsoluteThreshold = limitThreshold,
            InterestRateRelativeThreshold = rateThreshold
        };

        var result = MapToDetailedResult(snapshot.Id, replayResult, profile);

        return Ok(new
        {
            SnapshotId = id,
            Drift = result,
            Replay = replayResult
        });
    }

    private static RegressionResult MapToDetailedResult(Guid id, ReplayResult replay, DriftToleranceProfile profile)
    {
        var result = new RegressionResult
        {
            SnapshotId = id,
            LimitDrift = replay.DeltaCreditLimit,
            RateDrift = replay.DeltaInterestRate,
            TraceDriftDetected = replay.TraceDriftDetected
        };

        result.OutputSeverity = ClassifyOutputSeverity(result, profile);
        result.TraceSeverity = replay.TraceDriftDetected ? DriftSeverity.Significant : DriftSeverity.None;

        if (replay.Diff != null && replay.Diff.Mismatches.Count != 0)
        {
            result.Attribution = PerformDriftAttribution(replay.Diff);
            if (result.Attribution.MaxImpactDelta > profile.CreditLimitAbsoluteThreshold * 2)
            {
                result.TraceSeverity = DriftSeverity.Critical;
            }
        }

        return result;
    }

    private static DriftSeverity ClassifyOutputSeverity(RegressionResult result, DriftToleranceProfile profile)
    {
        var absLimit = Math.Abs(result.LimitDrift);
        var absRate = Math.Abs(result.RateDrift);

        return absLimit > profile.CreditLimitAbsoluteThreshold * 5 ||
               absRate > profile.InterestRateRelativeThreshold * 5
            ? DriftSeverity.Critical
            : absLimit > profile.CreditLimitAbsoluteThreshold || absRate > profile.InterestRateRelativeThreshold
                ? DriftSeverity.Significant
                : absLimit > 0 || absRate > 0
                    ? DriftSeverity.Minor
                    : DriftSeverity.None;
    }

    private static DriftAttribution PerformDriftAttribution(TraceDiff diff)
    {
        var attr = new DriftAttribution();
        if (diff.Mismatches.Count == 0) return attr;

        attr.FirstDriftStep = diff.Mismatches.First().StepName;
        var maxMismatch = diff.Mismatches.OrderByDescending(m => Math.Abs(m.ReplayedOutput - m.OriginalOutput)).First();
        attr.MaxImpactStep = maxMismatch.StepName;
        attr.MaxImpactDelta = Math.Abs(maxMismatch.ReplayedOutput - maxMismatch.OriginalOutput);
        attr.TotalImpact = diff.Mismatches.Sum(m => Math.Abs(m.ReplayedOutput - m.OriginalOutput));

        return attr;
    }
}

public class RunRegressionRequest
{
    public DriftToleranceProfile? Profile { get; set; }
    public string? BaselineVersion { get; set; }
    public int SampleRate { get; set; } = 100;
    public int? EarlyStopThreshold { get; set; }
}
