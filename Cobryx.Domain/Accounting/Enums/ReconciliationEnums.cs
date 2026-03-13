namespace Cobryx.Domain.Accounting.Enums;

public enum ReconciliationStatus
{
    Synced = 1,
    SoftDrift = 2,  // Within timing tolerance (Lag)
    HardDrift = 3,  // Out of sync or missing
    Repairing = 4,  // Auto-repair in progress
    Repaired = 5,    // Drift was detected and successfully fixed
    ConfirmedDrift = 6
}

public enum ReconciliationSeverity
{
    Info = 1,     // Minimal lag, expected
    Warning = 2,  // Drift detected but within thresholds
    Error = 3,    // Missing money or mismatch
    Critical = 4  // Balance mismatch or systemic failure
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
    LedgerCorruption = 8
}
