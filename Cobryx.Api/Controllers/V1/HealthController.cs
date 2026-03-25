using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Decision;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cobryx.Api.Controllers.V1;

[ApiController]
[Route("api/v1/health")]
public class HealthController(ICobryxDbContext db, HealthCheckService healthCheckService) : ControllerBase
{
    [HttpGet("drift")]
    public async Task<IActionResult> GetDriftHealth()
    {
        var oneHourAgo = DateTime.UtcNow.AddHours(-1);

        var totalExecutions = await db.DecisionSnapshots
            .CountAsync(s => s.CreatedAt >= oneHourAgo);

        if (totalExecutions == 0)
            return Ok(new { status = "healthy", driftRate = 0, criticalRate = 0, p95 = 0, nonComparable = 0, traceMismatch = 0, message = "No executions in the last hour" });

        var criticalDrifts = await db.ShadowDriftEvents
            .CountAsync(e => e.CreatedAt >= oneHourAgo && e.OutputSeverity == DriftSeverity.Critical);

        var traceMismatches = await db.ShadowDriftEvents
            .CountAsync(e => e.CreatedAt >= oneHourAgo && e.TraceSeverity == DriftSeverity.Critical);

        var significantDrifts = await db.ShadowDriftEvents
            .CountAsync(e => e.CreatedAt >= oneHourAgo && e.OutputSeverity == DriftSeverity.Significant);

        var nonComparables = await db.ShadowDriftEvents
            .CountAsync(e => e.CreatedAt >= oneHourAgo && e.TraceSeverity != DriftSeverity.None && e.TraceSeverity != DriftSeverity.Critical);

        var driftRate    = (double)(criticalDrifts + significantDrifts) / totalExecutions;
        var criticalRate = (double)criticalDrifts / totalExecutions;

        var status = criticalRate > 0.001 || traceMismatches > 0 ? "unhealthy"
                   : significantDrifts > 5 || nonComparables > 0  ? "degraded"
                   : "healthy";

        return Ok(new { status, driftRate, criticalRate, p95 = 0, nonComparable = nonComparables, traceMismatch = traceMismatches });
    }

    [HttpGet]
    public async Task<IActionResult> GetOverallHealth()
    {
        var report = await healthCheckService.CheckHealthAsync();
        return report.Status == HealthStatus.Healthy ? Ok(report) : StatusCode(503, report);
    }
}