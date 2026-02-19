using Asp.Versioning;
using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Contracts.V1.Financial;
using Cobryx.Api.Outcomes;
using Cobryx.Application.Common.Attributes;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Controller for general payment processing, allocation, and financial settlement.
/// Handles high-level payment registration before allocation to specific invoices or credit lines.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/financial/payments")]
[Tags("Financial Core")]
public class PaymentsController : CobryxBaseController
{
    public PaymentsController(ISender sender) : base(sender)
    {
    }

    /// <summary>
    /// Processes a new payment and handle optional allocation to one or more invoices.
    /// </summary>
    /// <param name="request">Payment transaction details including amount (Decimal, 2-digit precision) and ISO-4217 currency code.</param>
    /// <remarks>
    /// The 'Currency' field must be a valid 3-letter ISO-4217 code (e.g., USD, MXN, EUR).
    /// If invoice IDs are provided, the payment will be automatically allocated across them until exhausted.
    ///
    /// Possible Outcomes:
    /// - FINANCIAL.PAYMENT.COMPLETED: Payment successfully processed and allocated.
    /// - FINANCIAL.PAYMENT.FAILED: Processing error or invalid bank reference.
    /// </remarks>
    /// <response code="201">Returns the unique identifier for the registered payment.</response>
    /// <response code="400">Invalid amount, currency code, or malformed data.</response>
    /// <response code="422">Business rule violation (e.g., payment date in the future).</response>
    [HttpPost]
    [Idempotent]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 422)]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] ProcessPaymentRequest request)
    {
        // Intentional Mapping: Public Intent -> Internal Domain Command
        var command = new Application.Payments.Commands.ProcessPayment.ProcessPaymentCommand(
            request.CustomerId,
            request.PaymentMethodId,
            request.Amount,
            request.Currency,
            request.PaymentDate,
            request.Reference,
            request.Notes,
            request.InvoiceIds);

        var result = await Sender.Send(command);
        return HandleCreatedResult($"/api/v1/financial/payments/{result.Value}", result, InvoicingOutcomes.Payments.Completed);
    }
}
