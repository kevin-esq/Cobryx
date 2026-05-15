using System.Security.Claims;

using Asp.Versioning;

using Cobryx.Api.Attributes;
using Cobryx.Api.Outcomes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Observability.Queries.GetIdempotencyIntrospection;
using Cobryx.Application.Webhooks.Commands.ReplayWebhook;
using Cobryx.Application.Webhooks.Entities;
using Cobryx.Application.Webhooks.Queries.GetWebhookLogs;
using Cobryx.Domain.Shared;
using Cobryx.Domain.Accounting.Models;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Professional System &amp; Metasurveillance controller.
/// Handles operational introspection, idempotency resolution, and system status.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system")]
[Tags("System")]
public class SystemController(
    ISender sender,
    ILedgerIntegrityService integrityService,
    ILedgerHealthCache healthCache,
    ITenantProvider tenantProvider) : CobryxBaseController(sender)
{
    /// <summary>
    /// Returns the platform-wide technical health status including financial circuit breakers.
    /// Used by SRE dashboards to monitor degraded states.
    /// </summary>
    [HttpGet("health")]
    [AuthorizePermission("system.health.read")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    public IActionResult GetHealth()
    {
        Guid? tenantId = tenantProvider.GetTenantId();
        LedgerHealthStatus health = healthCache.Get(tenantId);

        var status = new
        {
            Status = health.IsSafeMode ? "DEGRADED" : "HEALTHY",
            Financial = new
            {
                SafeMode = health.IsSafeMode,
                Reason = health.Reason,
                Since = health.TriggeredAt,
                ExpiresAt = health.ExpiresAt
            },
            Timestamp = DateTime.UtcNow
        };

        return Success(status);
    }

    /// <summary>
    /// Executes a deep ledger integrity audit for the current tenant.
    /// If drift is detected, the tenant is automatically placed into Financial Safe Mode.
    /// </summary>
    [HttpPost("ledger/verify")]
    [AuthorizePermission("system.ledger.verify")]
    [ProducesResponseType(typeof(ApiSuccessResponse<IntegrityReport>), 200)]
    public async Task<IActionResult> VerifyLedger([FromQuery] bool forceFullReplay = false)
    {
        Guid? tenantId = tenantProvider.GetTenantId();
        IntegrityReport report = await integrityService.VerifyJournalIntegrityAsync(tenantId, forceFullReplay);

        Outcome outcome = report.IsHealthy
            ? SystemOutcomes.Ledger.IntegrityCheckCompleted
            : SystemOutcomes.Ledger.IntegrityCorruptionDetected;

        return Success(report, outcome);
    }
    /// <summary>
    /// Introspect the status and original response of an idempotency key.
    /// Used by integrators to resolve 504 Gateway Timeouts or network drops deterministically.
    /// </summary>
    /// <param name="key">The client-generated Idempotency-Key used in the original request.</param>
    /// <remarks>
    /// Responses are cached for 24 hours. If the key is not found, it is safe to re-send the original request.
    /// </remarks>
    /// <response code="200">Returns the current status (Processing/Completed/Failed) and original payload if available.</response>
    /// <response code="401">Unauthorized access.</response>
    /// <response code="404">Idempotency key not found.</response>
    [HttpGet("idempotency/{key}")]
    [AuthorizePermission("system.idempotency.read")]
    [ProducesResponseType(typeof(ApiSuccessResponse<IdempotencyIntrospectionDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetIdempotencyStatus(string key)
    {
        Result<IdempotencyIntrospectionDto> result = await Sender.Send(new GetIdempotencyIntrospectionQuery(key));
        return HandleResult(result, SystemOutcomes.Idempotency.IntrospectionRetrieved);
    }

    /// <summary>
    /// Retrieves recent incoming webhook logs for diagnostic visibility.
    /// </summary>
    /// <param name="status">Optional status filter (PENDING, PROCESSED, FAILED).</param>
    /// <param name="limit">Max number of logs to return (Default 50, Max 100).</param>
    /// <param name="offset">Number of logs to skip (for pagination).</param>
    [HttpGet("webhooks/logs")]
    [AuthorizePermission("webhooks.logs.read")]
    [ProducesResponseType(typeof(ApiSuccessResponse<IEnumerable<WebhookLogDto>>), 200)]
    public async Task<IActionResult> GetWebhookLogs(
        [FromQuery] WebhookStatus? status = null,
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0)
    {
        Result<IEnumerable<WebhookLogDto>> result = await Sender.Send(new GetWebhookLogsQuery(status, limit, offset));
        return HandleResult(result, SystemOutcomes.Webhooks.LogsRetrieved);
    }

    /// <summary>
    /// Manually re-triggers the processing of a specific webhook event.
    /// Supports 'forced' replays to bypass idempotency guards if deep reconciliation is needed.
    /// </summary>
    /// <param name="id">Internal identifier of the webhook event.</param>
    /// <param name="forced">If true, removes the domain-level idempotency record before replaying.</param>
    /// <param name="reason">Mandatory reason for the replay (especially if forced).</param>
    /// <param name="dryRun">If true, simulates the replay and returns expected outcome without committing changes.</param>
    [HttpPost("webhooks/{id}/replay")]
    [AuthorizePermission("webhooks.replay")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> ReplayWebhook(
        Guid id,
        [FromQuery] bool forced = false,
        [FromQuery] string? reason = null,
        [FromQuery] bool dryRun = false)
    {
        // Permission check for forced replay
        if (forced && !await HasPermissionAsync("webhooks.replay.forced"))
        {
            return Unauthorized();
        }

        Result result = await Sender.Send(new ReplayWebhookCommand(id, forced, reason, dryRun));
        return HandleResult(result, SystemOutcomes.Webhooks.Replayed);
    }

    private async Task<bool> HasPermissionAsync(string permission)
    {
        IPermissionService permissionService = HttpContext.RequestServices.GetRequiredService<IPermissionService>();
        Claim? userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId)) return false;
        return await permissionService.HasPermissionAsync(userId, permission);
    }
}
