using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Api.Services;
using Cobryx.Application.Common.Attributes;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Controller for general payment processing, allocation, and financial settlement.
/// Handles high-level payment registration before allocation to specific invoices or credit lines.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payments")]
[Tags("Payments")]
public class PaymentsController(ISender sender, IApiLinkGenerator linkGenerator) : CobryxBaseController(sender)
{
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
    [HttpPost]
    [Idempotent]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 422)]
    public async Task<IActionResult> ProcessPayment(
        [FromBody] ProcessPaymentRequest request)
    {
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
        return HandleCreatedResult(linkGenerator.GetPaymentUrl(result.Value), result,
            InvoicingOutcomes.Payments.Completed);
    }

    /// <summary>
    /// Retrieves a single payment record by its identifier.
    /// </summary>
    /// <param name="id">Unique identifier of the payment.</param>
    [HttpGet("{id}", Name = "GetPayment")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public IActionResult GetPayment(Guid id) =>
        Ok(ApiResponseFactory.Success(new
            { id, message = "Payment retrieval implemented via collection search currently." }));
}
