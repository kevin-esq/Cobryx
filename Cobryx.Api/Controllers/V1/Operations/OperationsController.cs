using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Application.Operations.Commands.ManualChargeOff;
using Cobryx.Application.Operations.Commands.ManualReversal;
using Cobryx.Application.Operations.Commands.SendTestAlert;
using Cobryx.Application.Operations.Commands.SuspendTenant;
using Cobryx.Application.Operations.Queries.GetFinancialMetrics;
using Cobryx.Application.Operations.Queries.GetLedgerHealth;
using Cobryx.Application.Operations.Queries.GetStripeReconciliation;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1.Operations;

/// <summary>
/// Business operations and tenant management endpoints.
/// These endpoints allow platform administrators to perform manual interventions,
/// monitor ledger health, and visualize platform metrics.
/// </summary>
[Authorize(Policy = "PlatformAdmin")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/operations")]
[Tags("Operations")]
public class OperationsController(ISender sender) : CobryxBaseController(sender)
{
    /// <summary>
    /// Returns the ledger health status including balance verification.
    /// </summary>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - ADMIN.HEALTH.LEDGER_RETRIEVED: Ledger health status retrieved.
    /// </remarks>
    [HttpGet("health/ledger")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> GetLedgerHealth(CancellationToken ct)
    {
        Result<LedgerHealthDto> result = await Sender.Send(new GetLedgerHealthQuery(), ct);
        return HandleResult(result, AdminOutcomes.Health.LedgerHealthRetrieved);
    }

    /// <summary>
    /// Returns aggregated financial metrics for the platform or a specific tenant.
    /// </summary>
    /// <param name="tenantId">Optional tenant filter.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - ADMIN.METRICS.RETRIEVED: Financial metrics retrieved.
    /// </remarks>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> GetMetrics([FromQuery] Guid? tenantId, CancellationToken ct)
    {
        Result<FinancialMetricsDto> result = await Sender.Send(new GetFinancialMetricsQuery(tenantId), ct);
        return HandleResult(result, AdminOutcomes.Metrics.Retrieved);
    }

    /// <summary>
    /// Returns Stripe reconciliation data for the platform or a specific tenant.
    /// </summary>
    /// <param name="tenantId">Optional tenant filter.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - ADMIN.RECONCILIATION.STRIPE_RETRIEVED: Stripe reconciliation data retrieved.
    /// </remarks>
    [HttpGet("reconciliation/stripe")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> GetStripeReconciliation([FromQuery] Guid? tenantId, CancellationToken ct)
    {
        Result<StripeReconciliationDto> result = await Sender.Send(new GetStripeReconciliationQuery(tenantId), ct);
        return HandleResult(result, AdminOutcomes.Reconciliation.StripeRetrieved);
    }

    /// <summary>
    /// Suspends a tenant account with a documented reason.
    /// </summary>
    /// <param name="id">The tenant identifier.</param>
    /// <param name="request">Suspension reason for audit trail.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - ADMIN.TENANT.SUSPENDED: Tenant suspended successfully.
    /// </remarks>
    [HttpPost("tenants/{id:guid}/suspend")]
    [Cobryx.Application.Common.Attributes.Idempotent]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> SuspendTenant(Guid id, [FromBody] AdminReasonRequest request, CancellationToken ct)
    {
        Result result = await Sender.Send(new SuspendTenantCommand(id, request.Reason), ct);
        return HandleResult(result, AdminOutcomes.Tenant.Suspended);
    }

    /// <summary>
    /// Manually reverses a transaction with a documented reason.
    /// </summary>
    /// <param name="id">The transaction identifier.</param>
    /// <param name="request">Reversal amount and reason for audit trail.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - ADMIN.TRANSACTION.REVERSED: Transaction reversed successfully.
    /// </remarks>
    [HttpPost("transactions/{id:guid}/reverse")]
    [Cobryx.Application.Common.Attributes.Idempotent]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> ManualReversal(Guid id, [FromBody] ManualReversalRequest request, CancellationToken ct)
    {
        Result result = await Sender.Send(new ManualReversalCommand(id, request.Amount, request.Reason), ct);
        return HandleResult(result, AdminOutcomes.Transaction.Reversed);
    }

    /// <summary>
    /// Manually charges off a loan with a documented reason.
    /// </summary>
    /// <param name="id">The loan identifier.</param>
    /// <param name="request">Charge-off reason for audit trail.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - ADMIN.LOAN.CHARGED_OFF: Loan charged off successfully.
    /// </remarks>
    [HttpPost("loans/{id:guid}/charge-off")]
    [Cobryx.Application.Common.Attributes.Idempotent]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> ManualChargeOff(Guid id, [FromBody] AdminReasonRequest request, CancellationToken ct)
    {
        Result result = await Sender.Send(new ManualChargeOffCommand(id, request.Reason), ct);
        return HandleResult(result, AdminOutcomes.Loan.ChargedOff);
    }

    /// <summary>
    /// Dispatches a test alert to verify the alerting pipeline.
    /// </summary>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <remarks>
    /// Possible Outcomes:
    /// - ADMIN.ALERT.TEST_SENT: Test alert dispatched.
    /// </remarks>
    [HttpPost("/api/v{version:apiVersion}/system/commands/test-alert")]
    [ProducesResponseType(typeof(ApiSuccessResponse), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> TestAlert(CancellationToken ct)
    {
        Result result = await Sender.Send(new SendTestAlertCommand(User.Identity?.Name), ct);
        return HandleResult(result, AdminOutcomes.Alert.TestSent);
    }
}

public record AdminReasonRequest(string Reason);
public record ManualReversalRequest(decimal Amount, string Reason);
