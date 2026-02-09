namespace Cobryx.Api.Outcomes;

/// <summary>
/// Status codes for the Lending domain operations.
/// </summary>
public static class LendingOutcomes
{
    public const string LoanCreated = "LENDING.LOAN.CREATED";
    public const string LoanSearchCompleted = "LENDING.LOAN.SEARCH_COMPLETED";
    public const string LoanScheduleRetrieved = "LENDING.LOAN.SCHEDULE_RETRIEVED";
    public const string PaymentRegistered = "LENDING.PAYMENT.REGISTERED";
    public const string LateFeesApplied = "LENDING.LATE_FEES.APPLIED";
    public const string LoanClosed = "LENDING.LOAN.CLOSED";
}
