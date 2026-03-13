namespace Cobryx.Domain.Accounting.Enums;

public enum FinancialEventType
{
    PaymentPosted,
    InterestAccrued,
    LateFeeApplied,
    LoanEnteredArrears,
    LoanDefaulted,
    LoanWriteOff,
    RecoveryPayment,
    BalanceUpdated,
    ReversalPosted
}
