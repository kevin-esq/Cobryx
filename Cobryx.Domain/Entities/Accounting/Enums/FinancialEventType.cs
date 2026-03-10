namespace Cobryx.Domain.Entities.Accounting.Enums;

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
