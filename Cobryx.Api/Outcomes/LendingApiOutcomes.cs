namespace Cobryx.Api.Outcomes;

/// <summary>
/// Status codes for the Lending domain operations (Contract Level).
/// </summary>
public static class LendingApiOutcomes
{
    // Success States
    public const string LoanCreated = "LENDING.LOAN.CREATE_SUCCESS";
    public const string LoanSearchCompleted = "LENDING.LOAN.SEARCH_SUCCESS";
    public const string LoanScheduleRetrieved = "LENDING.LOAN.SCHEDULE_SUCCESS";
    public const string PaymentRegistered = "LENDING.PAYMENT.REGISTER_SUCCESS";
    public const string LateFeesApplied = "LENDING.LATE_FEES.APPLY_SUCCESS";
    public const string LoanClosed = "LENDING.LOAN.CLOSE_SUCCESS";

    // Operational Failures
    public const string LoanCreationFailed = "LENDING.LOAN.CREATE_FAILED";
    public const string LoanValidationFailed = "LENDING.LOAN.VALIDATION_FAILED";
    public const string LoanPolicyViolation = "LENDING.LOAN.POLICY_VIOLATION";
    public const string LoanConflict = "LENDING.LOAN.CONFLICT_ERROR";

    public const string PaymentExceedsBalance = "LENDING.PAYMENT.OVERPAYMENT_ERROR";
    public const string CloseNotAllowed = "LENDING.LOAN.CLOSE_DENIED";
}
