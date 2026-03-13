using Asp.Versioning;

using Cobryx.Application.Customer.Commands.AttachPaymentMethod;
using Cobryx.Application.Customer.Commands.SetDefaultPaymentMethod;
using Cobryx.Application.Customer.Commands.ToggleAutoPay;
using Cobryx.Application.Customer.Queries.GetCustomerPaymentMethods;
using Cobryx.Application.Customer.Queries.GetCustomerStatement;
using Cobryx.Application.Portal.Queries.GetCustomerPortalSummary;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1.Portal;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/portal")]
[Tags("Customer Portal")]
public class PortalController : CobryxBaseController
{
    public PortalController(ISender sender) : base(sender)
    {
    }

    /// <summary>
    /// Gets a full summary for the customer portal: balances, active loans, and recent transactions.
    /// Ledger-backed for financial truth.
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiSuccessResponse<CustomerPortalSummaryDto>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var (customerId, tenantId) = GetCustomerContext();
        if (customerId == Guid.Empty) return Forbid();

        var result = await Sender.Send(new GetCustomerPortalSummaryQuery(customerId, tenantId), ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Generates a full financial statement for the customer.
    /// Includes loan balances and payment history.
    /// </summary>
    [HttpGet("statement")]
    [ProducesResponseType(typeof(ApiSuccessResponse<CustomerStatementDto>), 200)]
    public async Task<IActionResult> GetStatement(CancellationToken ct)
    {
        var (customerId, tenantId) = GetCustomerContext();
        if (customerId == Guid.Empty) return Forbid();

        var result = await Sender.Send(new GetCustomerStatementQuery(customerId, tenantId), ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Returns the list of saved payment methods for the authenticated customer.
    /// </summary>
    [HttpGet("payment-methods")]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<CustomerPaymentMethodDto>>), 200)]
    public async Task<IActionResult> GetPaymentMethods(CancellationToken ct)
    {
        var (customerId, _) = GetCustomerContext();
        if (customerId == Guid.Empty) return Forbid();

        var result = await Sender.Send(new GetCustomerPaymentMethodsQuery(customerId), ct);
        return HandleResult(result);
    }

    [HttpPost("payment-methods/attach")]
    [ProducesResponseType(typeof(ApiSuccessResponse<Result>), 200)]
    public async Task<IActionResult> AttachPaymentMethod([FromBody] AttachPaymentMethodRequest request, CancellationToken ct)
    {
        var (customerId, _) = GetCustomerContext();
        if (customerId == Guid.Empty) return Forbid();

        var result = await Sender.Send(new AttachPaymentMethodCommand(customerId, request.PaymentMethodId), ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Sets a saved payment method as the default for AutoPay and future charges.
    /// </summary>
    [HttpPost("payment-methods/default")]
    [ProducesResponseType(typeof(ApiSuccessResponse<Result>), 200)]
    public async Task<IActionResult> SetDefaultPaymentMethod([FromBody] SetDefaultPaymentMethodRequest request, CancellationToken ct)
    {
        var (customerId, _) = GetCustomerContext();
        if (customerId == Guid.Empty) return Forbid();

        var result = await Sender.Send(new SetDefaultPaymentMethodCommand(customerId, request.PaymentMethodId), ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Toggles the AutoPay (Automatic Collection) state for the customer.
    /// </summary>
    [HttpPost("autopay/toggle")]
    [ProducesResponseType(typeof(ApiSuccessResponse<Result>), 200)]
    public async Task<IActionResult> ToggleAutoPay([FromBody] ToggleAutoPayRequest request, CancellationToken ct)
    {
        var (customerId, _) = GetCustomerContext();
        if (customerId == Guid.Empty) return Forbid();

        var result = await Sender.Send(new ToggleAutoPayCommand(customerId, request.Enabled), ct);
        return HandleResult(result);
    }

    private (Guid CustomerId, Guid TenantId) GetCustomerContext()
    {
        var customerIdClaim = User.FindFirst("customer_id")?.Value;
        var tenantIdClaim = User.FindFirst(CobryxClaimTypes.TenantId)?.Value;

        if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId) ||
            string.IsNullOrEmpty(tenantIdClaim) || !Guid.TryParse(tenantIdClaim, out var tenantId))
        {
            return (Guid.Empty, Guid.Empty);
        }

        return (customerId, tenantId);
    }
}

public record AttachPaymentMethodRequest(string PaymentMethodId);
public record SetDefaultPaymentMethodRequest(string PaymentMethodId);
public record ToggleAutoPayRequest(bool Enabled);
