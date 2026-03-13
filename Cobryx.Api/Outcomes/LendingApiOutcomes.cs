using Cobryx.Domain.Shared;

namespace Cobryx.Api.Outcomes;

/// <summary>
/// Status codes for the Lending domain operations (Contract Level).
/// </summary>
public static class LendingApiOutcomes
{
    private const string Prefix = "LENDING";

    // Success States
    public static readonly Outcome LoanCreated = new($"{Prefix}.LOAN.CREATE_SUCCESS", OutcomeCategory.Success, "Loan created successfully.");
    public static readonly Outcome LoanSearchCompleted = new($"{Prefix}.LOAN.SEARCH_SUCCESS", OutcomeCategory.Success, "Loan search completed.");
    public static readonly Outcome LoanScheduleRetrieved = new($"{Prefix}.LOAN.SCHEDULE_SUCCESS", OutcomeCategory.Success, "Loan schedule retrieved successfully.");
    public static readonly Outcome PaymentRegistered = new($"{Prefix}.PAYMENT.REGISTER_SUCCESS", OutcomeCategory.Success, "Payment registered successfully.");
    public static readonly Outcome LateFeesApplied = new($"{Prefix}.LATE_FEES.APPLY_SUCCESS", OutcomeCategory.Success, "Late fees applied successfully.");
    public static readonly Outcome LoanClosed = new($"{Prefix}.LOAN.CLOSE_SUCCESS", OutcomeCategory.Success, "Loan closed successfully.");

    // Operational Failures
    public static readonly Outcome LoanCreationFailed = new($"{Prefix}.LOAN.CREATE_FAILED", OutcomeCategory.BusinessError, "Loan creation failed.");
    public static readonly Outcome LoanValidationFailed = new($"{Prefix}.LOAN.VALIDATION_FAILED", OutcomeCategory.BusinessError, "Loan validation failed.");
    public static readonly Outcome LoanPolicyViolation = new($"{Prefix}.LOAN.POLICY_VIOLATION", OutcomeCategory.BusinessError, "Loan policy violation.");
    public static readonly Outcome LoanConflict = new($"{Prefix}.LOAN.CONFLICT_ERROR", OutcomeCategory.BusinessError, "Loan conflict occurred.");

    public static readonly Outcome PaymentExceedsBalance = new($"{Prefix}.PAYMENT.OVERPAYMENT_ERROR", OutcomeCategory.BusinessError, "Payment exceeds balance.");
    public static readonly Outcome CloseNotAllowed = new($"{Prefix}.LOAN.CLOSE_DENIED", OutcomeCategory.BusinessError, "Closing loan is not allowed.");
}
