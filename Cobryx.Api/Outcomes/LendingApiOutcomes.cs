namespace Cobryx.Api.Outcomes;

/// <summary>
/// Status codes for the Lending domain operations (Contract Level).
/// </summary>
public static class LendingApiOutcomes
{
    // Success States
    public const string LoanCreated = "LENDING.LOAN.CREATED";
    public const string LoanSearchCompleted = "LENDING.LOAN.SEARCH_COMPLETED";
    public const string LoanScheduleRetrieved = "LENDING.LOAN.SCHEDULE_RETRIEVED";
    public const string PaymentRegistered = "LENDING.PAYMENT.REGISTERED";
    public const string LateFeesApplied = "LENDING.LATE_FEES.APPLIED";
    public const string LoanClosed = "LENDING.LOAN.CLOSED";

    // Strategic Failures (Enterprise Polish)
    public const string LoanCreationFailed = "LENDING.LOAN.CREATION_FAILED";
    public const string LoanValidationFailed = "LENDING.LOAN.VALIDATION_FAILED";
    public const string LoanPolicyViolation = "LENDING.LOAN.POLICY_VIOLATION";
    public const string LoanConflict = "LENDING.LOAN.CONFLICT";

    public const string PaymentExceedsBalance = "LENDING.PAYMENT.EXCEEDS_BALANCE";
    public const string CloseNotAllowed = "LENDING.LOAN.CLOSE_NOT_ALLOWED";
}
