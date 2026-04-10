namespace Cobryx.Domain.Accounting.Enums;

public enum ReconciliationStatus
{
    Synced = 1,
    SoftDrift = 2,
    HardDrift = 3,
    Repairing = 4,
    Repaired = 5,
    ConfirmedDrift = 6
}

public enum ReconciliationSeverity
{
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}

public enum DriftType
{
    None = 0,
    MissingPayment = 1,
    AmountMismatch = 2,
    OrphanLedger = 3,
    TimingLag = 4,
    BalanceMismatch = 5,
    FeeMismatch = 6,
    PayoutMismatch = 7,
    LedgerCorruption = 8,
    PhantomPayment = 9,      // DB has payment, Stripe doesn't (refund/chargeback)
    InvariantViolation = 10  // Ledger invariants broken (debits != credits)
}
