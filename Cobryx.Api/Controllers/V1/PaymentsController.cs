using Asp.Versioning;

using Cobryx.Api.Outcomes;
using Cobryx.Api.Services;
using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Payments.Commands.RefundPayment;
using Cobryx.Application.Payments.Commands.RetryPayment;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using ProcessPaymentCommand = Cobryx.Application.Payments.Commands.ProcessPayment.ProcessPaymentCommand;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Handles payment processing, allocation, and financial settlement.
/// Manages high-level payment registration before allocation to specific invoices or credit lines.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payments")]
[Tags("Payments")]
public class PaymentsController(ISender sender, IApiLinkGenerator linkGenerator) : CobryxBaseController(sender)
{
    /// <summary>
    /// Processes a new payment with optional allocation to one or more invoices.
    /// </summary>
    /// <param name="request">Payment details including amount (2-digit decimal precision) and ISO-4217 currency code.</param>
    /// <remarks>
    /// The <c>Currency</c> field must be a valid 3-letter ISO-4217 code (e.g., USD, MXN, EUR).
    /// If invoice IDs are provided, the payment will be automatically allocated across them until exhausted.
    ///
    /// Possible Outcomes:
    /// - FINANCIAL.PAYMENT.COMPLETED: Payment successfully processed and allocated.
    /// - FINANCIAL.PAYMENT.FAILED: Processing error or invalid bank reference.
    /// </remarks>
    /// <response code="201">Returns the unique identifier for the registered payment.</response>
    /// <response code="400">Invalid amount, currency code, or malformed data.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="422">Business rule violation (e.g., payment date in the future).</response>
    [HttpPost]
    [Idempotent]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        var command = new ProcessPaymentCommand(
            request.CustomerId,
            request.PaymentMethodId,
            request.Amount,
            request.Currency,
            request.PaymentDate,
            request.Reference,
            request.Notes,
            request.InvoiceIds);

        Result<Guid> result = await Sender.Send(command);

        return HandleCreatedResult(linkGenerator.GetPaymentUrl(result.Value), result,
            InvoicingOutcomes.Payments.Completed);
    }

    /// <summary>
    /// Reverses a payment and returns funds to the original payment method.
    /// Supports partial refunds and requires explicit justification.
    /// </summary>
    /// <param name="id">Identifier of the payment to refund.</param>
    /// <param name="request">Refund details including optional partial amount and reason.</param>
    /// <response code="200">Payment successfully refunded.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="404">Payment not found.</response>
    /// <response code="422">Refund exceeds available balance or payment is in an invalid state.</response>
    [HttpPost("{id}/refund")]
    [Idempotent]
    [ProducesResponseType(typeof(ApiSuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RefundPayment(Guid id, [FromBody] RefundPaymentRequest request)
    {
        var refundAmount = request.Amount ?? 0;

        Result result = await Sender.Send(new RefundPaymentCommand(
            id,
            refundAmount,
            request.Currency,
            request.Reason));

        return HandleResult(result, InvoicingOutcomes.Payments.Refunded);
    }

    /// <summary>
    /// Re-triggers a previously failed payment attempt.
    /// Requires an attempt number to distinguish between multiple retry events.
    /// </summary>
    /// <param name="id">Identifier of the payment to retry.</param>
    /// <param name="request">Retry details including attempt number.</param>
    /// <response code="202">Retry request accepted and processing started.</response>
    /// <response code="401">Unauthorized.</response>
    /// <response code="409">Payment is already being processed or retried.</response>
    /// <response code="422">Payment is not in a failed state.</response>
    [HttpPost("{id}/retry")]
    [Idempotent]
    [ProducesResponseType(typeof(ApiSuccessResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RetryPayment(Guid id, [FromBody] RetryPaymentRequest request)
    {
        Result result = await Sender.Send(new RetryPaymentCommand(id, request.AttemptNumber));
        return HandleAcceptedResult(result, InvoicingOutcomes.Payments.Retrying);
    }

    /// <summary>
    /// Retrieves a single payment record by its identifier.
    /// </summary>
    /// <param name="id">Unique identifier of the payment.</param>
    /// <response code="200">Payment record found.</response>
    /// <response code="404">Payment not found.</response>
    /// <remarks>
    /// ⚠️ This endpoint is a stub. Full implementation via a dedicated query is pending.
    /// </remarks>
    [HttpGet("{id}", Name = "GetPayment")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public IActionResult GetPayment(Guid id) =>
        Ok(ApiResponseFactory.Success(new { id, message = "Stub: payment retrieval via dedicated query pending." }));
}
