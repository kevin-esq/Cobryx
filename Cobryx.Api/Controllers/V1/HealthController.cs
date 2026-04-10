using Asp.Versioning;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Decision;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Provides system health endpoints for infrastructure monitoring and drift analysis.
/// </summary>
/// <remarks>
/// Health endpoints are intentionally kept on <see cref="ControllerBase"/> (not CobryxBaseController)
/// because they return provider-specific response shapes, not domain Result types.
/// MediatR is not used here because health checks are stateless, read-only infrastructure queries
/// with no domain logic.
/// </remarks>
[AllowAnonymous]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/health")]
[Tags("System")]
public class HealthController(
    ICobryxDbContext db,
    IClock clock,
    HealthCheckService healthCheckService) : ControllerBase
{
    /// <summary>
    /// Returns the drift health status based on shadow execution analysis in the last hour.
    /// </summary>
    /// <response code="200">Drift analysis report with status (healthy, degraded, unhealthy).</response>
    [HttpGet("drift")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> GetDriftHealth()
    {
        DateTime oneHourAgo = clock.UtcNow.AddHours(-1);

        var totalExecutions = await db.DecisionSnapshots
            .CountAsync(s => s.CreatedAt >= oneHourAgo);

        if (totalExecutions == 0)
        {
            return Ok(new
            {
                status = "healthy",
                driftRate = 0,
                criticalRate = 0,
                p95 = 0,
                nonComparable = 0,
                traceMismatch = 0,
                message = "No executions in the last hour"
            });
        }

        var criticalDrifts = await db.ShadowDriftEvents
            .CountAsync(e => e.CreatedAt >= oneHourAgo && e.OutputSeverity == DriftSeverity.Critical);

        var traceMismatches = await db.ShadowDriftEvents
            .CountAsync(e => e.CreatedAt >= oneHourAgo && e.TraceSeverity == DriftSeverity.Critical);

        var significantDrifts = await db.ShadowDriftEvents
            .CountAsync(e => e.CreatedAt >= oneHourAgo && e.OutputSeverity == DriftSeverity.Significant);

        var nonComparables = await db.ShadowDriftEvents
            .CountAsync(e =>
                e.CreatedAt >= oneHourAgo && e.TraceSeverity != DriftSeverity.None &&
                e.TraceSeverity != DriftSeverity.Critical);

        var driftRate = (double)(criticalDrifts + significantDrifts) / totalExecutions;
        var criticalRate = (double)criticalDrifts / totalExecutions;

        var status = criticalRate > 0.001 || traceMismatches > 0 ? "unhealthy"
            : significantDrifts > 5 || nonComparables > 0 ? "degraded"
            : "healthy";

        return Ok(new
        {
            status,
            driftRate,
            criticalRate,
            p95 = 0,
            nonComparable = nonComparables,
            traceMismatch = traceMismatches
        });
    }

    /// <summary>
    /// Returns the overall system health status from registered health checks.
    /// </summary>
    /// <response code="200">All health checks passed.</response>
    /// <response code="503">One or more health checks failed.</response>
    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(503)]
    public async Task<IActionResult> GetOverallHealth()
    {
        HealthReport report = await healthCheckService.CheckHealthAsync();
        return report.Status == HealthStatus.Healthy
            ? Ok(report)
            : StatusCode(503, report);
    }
}
