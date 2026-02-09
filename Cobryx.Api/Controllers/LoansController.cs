using Cobryx.Application.Lending.Commands.CreateLoan;
using Cobryx.Application.Lending.Commands.RegisterPayment;
using Cobryx.Application.Lending.Commands.ApplyLateFees;
using Cobryx.Application.Lending.Commands.CloseLoan;
using Cobryx.Application.Lending.Queries.GetLoanSchedule;
using Cobryx.Api.Outcomes;
using Concordia;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cobryx.Api.Controllers;

/// <summary>
/// Controller for managing loan lifecycles and operations.
/// </summary>
[Authorize]
[ApiController]
[Route("api/lending/loans")]
public class LoansController : CobryxBaseController
{
    public LoansController(ISender sender) : base(sender)
    {
    }

    /// <summary>
    /// Creates a new loan agreement and activates the loan.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CanCreateCredits")]
    public async Task<IActionResult> Create(CreateLoanCommand command)
    {
        var result = await Sender.Send(command);
        return HandleCreatedResult($"/api/lending/loans/{result.Value}/schedule", result, LendingOutcomes.LoanCreated);
    }

    /// <summary>
    /// Retrieves the amortization schedule and current status of a loan.
    /// </summary>
    [HttpGet("{id}/schedule")]
    [Authorize(Policy = "CanViewCredits")]
    public async Task<IActionResult> GetSchedule(Guid id)
    {
        var result = await Sender.Send(new GetLoanScheduleQuery(id));
        return HandleResult(result, LendingOutcomes.LoanScheduleRetrieved);
    }

    /// <summary>
    /// Registers a payment and applies it to the loan components.
    /// </summary>
    [HttpPost("{id}/payments")]
    [Authorize(Policy = "CanApplyPayments")]
    public async Task<IActionResult> RegisterPayment(Guid id, [FromBody] RegisterPaymentRequest request)
    {
        var command = new RegisterPaymentCommand(
            id,
            request.Amount,
            request.PaymentMethodId,
            request.PaidAt,
            request.Reference,
            request.Notes);

        var result = await Sender.Send(command);
        return HandleResult(result, LendingOutcomes.PaymentRegistered);
    }

    /// <summary>
    /// Formally closes a loan if all balances are zero.
    /// </summary>
    [HttpPost("{id}/close")]
    [Authorize(Policy = "CanCreateCredits")]
    public async Task<IActionResult> Close(Guid id)
    {
        var result = await Sender.Send(new CloseLoanCommand(id));
        return HandleResult(result, LendingOutcomes.LoanClosed);
    }

    /// <summary>
    /// Trigger late fee assessment for overdue installments.
    /// If LoanId is null, it processes all active loans for the tenant.
    /// </summary>
    [HttpPost("late-fees")]
    [Authorize(Policy = "CanManageTenant")]
    public async Task<IActionResult> ApplyLateFees([FromQuery] Guid? loanId)
    {
        var result = await Sender.Send(new ApplyLateFeesCommand(loanId));
        return HandleResult(result, LendingOutcomes.LateFeesApplied);
    }
}

/// <summary>
/// Request DTO for registering a payment via API.
/// </summary>
public record RegisterPaymentRequest(
    decimal Amount,
    Guid PaymentMethodId,
    DateTime PaidAt,
    string? Reference = null,
    string? Notes = null
);
