using Asp.Versioning;

using Cobryx.Application.Admin.Commands.ManualChargeOff;
using Cobryx.Application.Admin.Commands.ManualReversal;
using Cobryx.Application.Admin.Commands.SuspendTenant;
using Cobryx.Application.Admin.Queries.GetFinancialMetrics;
using Cobryx.Application.Admin.Queries.GetLedgerHealth;
using Cobryx.Application.Admin.Queries.GetStripeReconciliation;
using Cobryx.Application.Common.Interfaces;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1.Admin;

[Authorize(Policy = "PlatformAdmin")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
[Tags("Platform Administration")]
public class AdminController : CobryxBaseController
{
    public AdminController(ISender sender) : base(sender) { }

    [HttpGet("health/ledger")]
    public async Task<IActionResult> GetLedgerHealth(CancellationToken ct)
    {
        var result = await Sender.Send(new GetLedgerHealthQuery(), ct);
        return HandleResult(result);
    }

    [HttpGet("metrics")]
    public async Task<IActionResult> GetMetrics([FromQuery] Guid? tenantId, CancellationToken ct)
    {
        var result = await Sender.Send(new GetFinancialMetricsQuery(tenantId), ct);
        return HandleResult(result);
    }

    [HttpGet("reconciliation/stripe")]
    public async Task<IActionResult> GetStripeReconciliation([FromQuery] Guid? tenantId, CancellationToken ct)
    {
        var result = await Sender.Send(new GetStripeReconciliationQuery(tenantId), ct);
        return HandleResult(result);
    }

    [HttpPost("tenants/{id}/suspend")]
    public async Task<IActionResult> SuspendTenant(Guid id, [FromBody] AdminReasonRequest request, CancellationToken ct)
    {
        var result = await Sender.Send(new SuspendTenantCommand(id, request.Reason), ct);
        return HandleResult(result);
    }

    [HttpPost("transactions/{id}/reverse")]
    public async Task<IActionResult> ManualReversal(Guid id, [FromBody] ManualReversalRequest request, CancellationToken ct)
    {
        var result = await Sender.Send(new ManualReversalCommand(id, request.Amount, request.Reason), ct);
        return HandleResult(result);
    }

    [HttpPost("loans/{id}/charge-off")]
    public async Task<IActionResult> ManualChargeOff(Guid id, [FromBody] AdminReasonRequest request, CancellationToken ct)
    {
        var result = await Sender.Send(new ManualChargeOffCommand(id, request.Reason), ct);
        return HandleResult(result);
    }

    [HttpPost("alerts/test")]
    public async Task<IActionResult> TestAlert([FromServices] IAlertingService alertingService, CancellationToken ct)
    {
        await alertingService.SendAlertAsync(
            "AdminConsole",
            "This is a manual test alert to verify the communication pipeline.",
            AlertLevel.Info,
            new { AdminUser = User.Identity?.Name, Timestamp = DateTime.UtcNow },
            ct);

        return Ok(new { Message = "Test alert dispatched." });
    }
}

public record AdminReasonRequest(string Reason);
public record ManualReversalRequest(decimal Amount, string Reason);
