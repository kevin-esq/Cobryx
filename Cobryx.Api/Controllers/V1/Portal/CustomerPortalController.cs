using Asp.Versioning;

using Cobryx.Application.Customers.Commands.PaymentMethods;
using Cobryx.Application.Customers.Queries.Account;
using Cobryx.Application.Portal.Queries.GetCustomerPortalSummary;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1.Portal;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customer-portal")]
[Tags("Customer Portal")]
public class CustomerPortalController(ISender sender) : CobryxBaseController(sender)
{
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
        if (customerId == Guid.Empty)
            return Forbid();

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
        if (customerId == Guid.Empty)
            return Forbid();

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
        if (customerId == Guid.Empty)
            return Forbid();

        var result = await Sender.Send(new GetCustomerPaymentMethodsQuery(customerId), ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Attaches a new payment method to the customer account using a provider-specific token.
    /// </summary>
    /// <param name="request">The payment method identifier or token from the provider (e.g., Stripe).</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <response code="200">Payment method successfully attached and verified.</response>
    [HttpPost("payment-methods")]
    [ProducesResponseType(typeof(ApiSuccessResponse<Result>), 200)]
    public async Task<IActionResult> AttachPaymentMethod([FromBody] AttachPaymentMethodRequest request,
        CancellationToken ct)
    {
        var (customerId, _) = GetCustomerContext();
        if (customerId == Guid.Empty)
            return Forbid();

        var result = await Sender.Send(new AttachPaymentMethodCommand(customerId, request.PaymentMethodId), ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Sets a saved payment method as the default for AutoPay and future charges.
    /// </summary>
    /// <param name="request">The internal identifier of the saved payment method.</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <response code="200">Default payment method updated.</response>
    [HttpPatch("payment-methods/default")]
    [ProducesResponseType(typeof(ApiSuccessResponse<Result>), 200)]
    public async Task<IActionResult> SetDefaultPaymentMethod([FromBody] SetDefaultPaymentMethodRequest request,
        CancellationToken ct)
    {
        var (customerId, _) = GetCustomerContext();
        if (customerId == Guid.Empty)
            return Forbid();

        var result = await Sender.Send(new SetDefaultPaymentMethodCommand(customerId, request.PaymentMethodId), ct);
        return HandleResult(result);
    }

    /// <summary>
    /// Updates the AutoPay (Automatic Collection) state for the customer.
    /// </summary>
    /// <param name="request">The desired state (Enabled/Disabled).</param>
    /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
    /// <response code="200">AutoPay configuration updated.</response>
    [HttpPatch("autopay")]
    [ProducesResponseType(typeof(ApiSuccessResponse<Result>), 200)]
    public async Task<IActionResult> UpdateAutoPay([FromBody] UpdateAutoPayRequest request, CancellationToken ct)
    {
        var (customerId, _) = GetCustomerContext();
        if (customerId == Guid.Empty)
            return Forbid();

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

public record UpdateAutoPayRequest(bool Enabled);
