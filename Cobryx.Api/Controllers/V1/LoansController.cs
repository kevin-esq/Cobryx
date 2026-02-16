using Asp.Versioning;
using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Contracts.V1.Lending;
using Cobryx.Api.Outcomes;
using Cobryx.Domain.Entities.Lending.Enums;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cobryx.Api.Controllers.V1;

/// <summary>
/// Controller for managing loan agreements, amortization schedules, and registration of financial payments.
/// Addresses the core lending lifecycle from activation to formal closure.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/lending/loans")]
[Tags("Financial Core")]
public class LoansController : CobryxBaseController
{
    public LoansController(ISender sender) : base(sender)
    {
    }

    /// <summary>
    /// Creates a new loan agreement, generates a tentative amortization schedule, and activates the credit line.
    /// </summary>
    /// <param name="request">The loan configuration including principal amount (Decimal, 2-digit precision) and terms.</param>
    /// <param name="idempotencyKey">Informational only — replay protection is not yet enforced. Pass a UUID to prepare for future deduplication.</param>
    /// <remarks>
    /// Financial Precision:
    /// - 'Amount' should be provided in the native currency unit (ISO-4217).
    /// - 'InterestRate' is treated as an Annual Nominal Rate (APR).
    /// - 'PaymentFrequency' supports: 'Weekly', 'BiWeekly', 'Monthly'.
    ///
    /// All date fields must follow ISO-8601 format.
    ///
    /// Possible Outcomes:
    /// - LENDING.LOAN.CREATED: Agreement finalized and activated successfully.
    /// - LENDING.LOAN.CREATION_FAILED: Validation error (e.g., negative amount, invalid installments).
    /// </remarks>
    /// <response code="201">Returns the unique identifier for the newly created resource.</response>
    /// <response code="400">Invalid request parameters or malformed JSON.</response>
    /// <response code="422">Business rule violation (e.g., policy invalid or status not allowed).</response>
    [HttpPost]
    [Authorize(Policy = "CanCreateCredits")]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 422)]
    public async Task<IActionResult> Create(
        [FromBody] CreateLoanRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
        // Intentional Mapping: Public Request -> Internal Domain Command
        var command = new Application.Lending.Commands.CreateLoan.CreateLoanCommand(
            request.CustomerId,
            request.Amount,
            Enum.Parse<PaymentFrequency>(request.PaymentFrequency, true),
            request.InstallmentsCount,
            request.InterestPolicyCode,
            request.LateFeePolicyCode,
            request.PaymentApplicationPolicyCode,
            Enum.Parse<LoanOrigin>(request.Origin, true),
            request.FirstDueDate,
            request.ReferenceId);

        var result = await Sender.Send(command);
        return HandleCreatedResult($"/api/v1/lending/loans/{result.Value}/schedule", result, LendingApiOutcomes.LoanCreated);
    }

    /// <summary>
    /// Retrieves the full amortization schedule, including principal targets and projected interest.
    /// </summary>
    /// <param name="id">Unique identifier of the loan.</param>
    /// <remarks>
    /// This endpoint provides the current financial snapshot of the agreement.
    ///
    /// Possible Outcomes:
    /// - LENDING.LOAN.SCHEDULE_RETRIEVED: Full schedule retrieved.
    /// - SYSTEM.OPERATION.FAILED: Loan record not found.
    /// </remarks>
    /// <response code="200">The current amortization schedule.</response>
    /// <response code="404">Resource not found.</response>
    [HttpGet("{id}/schedule")]
    [Authorize(Policy = "CanViewCredits")]
    [ProducesResponseType(typeof(ApiSuccessResponse<AmortizationScheduleContract>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    public async Task<IActionResult> GetSchedule(Guid id)
    {
        var result = await Sender.Send(new Application.Lending.Queries.GetLoanSchedule.GetLoanScheduleQuery(id));

        if (!result.IsSuccess || result.Value == null)
        {
            return HandleResult(result, LendingApiOutcomes.LoanScheduleRetrieved);
        }

        // Intentional Mapping: Application DTO -> Public Contract
        var mapped = new AmortizationScheduleContract(
            result.Value.LoanId,
            result.Value.LoanNumber,
            result.Value.Status.ToString(),
            result.Value.TotalPrincipal,
            result.Value.TotalInterest,
            result.Value.TotalPaid,
            result.Value.Installments.Select(i => new AmortizationInstallmentContract(
                i.Id,
                i.InstallmentNumber,
                i.DueDate,
                i.PrincipalAmount,
                i.InterestAmount,
                i.TotalAmount,
                i.PrincipalPaid,
                i.InterestPaid,
                i.LateFeesPaid,
                i.TotalPaid,
                i.RemainingAmount,
                i.Status.ToString(),
                i.PaidAt)).ToList());

        return Success(mapped, LendingApiOutcomes.LoanScheduleRetrieved);
    }

    /// <summary>
    /// Registers a customer payment against a loan and handles automatic allocation across loan components.
    /// </summary>
    /// <param name="id">Unique identifier of the target loan.</param>
    /// <param name="request">Payment details (Amount in native currency unit [ISO-4217], Payment Date, and References).</param>
    /// <param name="idempotencyKey">Informational only — replay protection is not yet enforced. Pass a UUID to prepare for future deduplication.</param>
    /// <remarks>
    /// Amounts are applied according to the loan's 'PaymentApplicationPolicy' (typically Principal -> Interest -> Fees).
    ///
    /// Possible Outcomes:
    /// - LENDING.PAYMENT.REGISTERED: Payment successfully stored and allocated.
    /// - LENDING.PAYMENT.EXCEEDS_BALANCE: Attempted to pay more than the outstanding debt.
    /// - LENDING.PAYMENT.FAILED: Internal calculation or validation failure.
    /// </remarks>
    /// <response code="201">Returns the unique identifier for the registered payment resource.</response>
    /// <response code="400">Invalid amount or invalid payment method.</response>
    /// <response code="404">Target loan not found.</response>
    /// <response code="422">Business rule violation (e.g., payment date in the future or closed loan).</response>
    [HttpPost("{id}/payments")]
    [Authorize(Policy = "CanApplyPayments")]
    [ProducesResponseType(typeof(ApiSuccessResponse<Guid>), 201)]
    [ProducesResponseType(typeof(ApiErrorResponse), 400)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 404)]
    [ProducesResponseType(typeof(ApiErrorResponse), 422)]
    public async Task<IActionResult> RegisterPayment(
        Guid id,
        [FromBody] LoanPaymentRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
    {
        // Intentional Mapping: Public Request -> Internal Domain Command
        var command = new Application.Lending.Commands.RegisterPayment.RegisterPaymentCommand(
            id,
            request.Amount,
            request.PaymentMethodId,
            request.PaidAt,
            request.Reference,
            request.Notes);

        var result = await Sender.Send(command);
        return HandleCreatedResult($"/api/v1/lending/loans/{id}/payments/{result.Value}", result, LendingApiOutcomes.PaymentRegistered);
    }

    /// <summary>
    /// Formally closes a loan agreement and prevents further mutations.
    /// </summary>
    /// <param name="id">Unique identifier of the loan.</param>
    /// <remarks>
    /// This PATCH endpoint represents a well-defined state transition (command), not a partial update of arbitrary fields.
    ///
    /// A loan can only be closed if all principal, interest, and fee balances are zero.
    ///
    /// Idempotency: This operation is idempotent only when the loan is already closed.
    ///
    /// Possible Outcomes:
    /// - LENDING.LOAN.CLOSED: Loan facility deactivated.
    /// - LENDING.LOAN.FAILED: Identification error or status conflict.
    /// </remarks>
    /// <response code="200">Loan successfully closed.</response>
    /// <response code="422">Operation denied due to business rules.</response>
    [HttpPatch("{id}/close")]
    [Authorize(Policy = "CanCreateCredits")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    [ProducesResponseType(typeof(ApiErrorResponse), 422)]
    public async Task<IActionResult> Close(Guid id)
    {
        var result = await Sender.Send(new Application.Lending.Commands.CloseLoan.CloseLoanCommand(id));
        return HandleResult(result, LendingApiOutcomes.LoanClosed);
    }

    /// <summary>
    /// ADMIN ONLY: Trigger late fee assessment across the lending portfolio.
    /// </summary>
    /// <param name="loanId">Optional identifier to process a single loan. If omitted, all overdue loans are processed.</param>
    /// <remarks>
    /// ⚠️ This endpoint performs a batch operation that mutates financial state across multiple loans.
    /// Intended for administrative/system use only — not for external integrators.
    ///
    /// This is a high-cost mutation that iterates over all active agreements. Use with caution.
    ///
    /// Possible Outcomes:
    /// - LENDING.LATE_FEES.APPLIED: Batch operation completed successfully.
    /// - SYSTEM.OPERATION.FAILED: System configuration or domain error.
    /// </remarks>
    /// <response code="200">Batch processing finished.</response>
    /// <response code="403">Forbidden (Admin only).</response>
    [HttpPost("late-fees")]
    [Authorize(Policy = "CanManageTenant")]
    [Tags("System & Administration")]
    [ProducesResponseType(typeof(ApiSuccessResponse<object>), 200)]
    [ProducesResponseType(typeof(ApiErrorResponse), 401)]
    [ProducesResponseType(typeof(ApiErrorResponse), 403)]
    public async Task<IActionResult> ApplyLateFees([FromQuery] Guid? loanId)
    {
        var result = await Sender.Send(new Application.Lending.Commands.ApplyLateFees.ApplyLateFeesCommand(loanId));
        return HandleResult(result, LendingApiOutcomes.LateFeesApplied);
    }
}
