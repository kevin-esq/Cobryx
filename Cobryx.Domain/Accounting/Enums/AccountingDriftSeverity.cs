namespace Cobryx.Domain.Accounting.Enums;

/// <summary>
/// Severity levels for financial ledger discrepancies detection.
/// </summary>
public enum AccountingDriftSeverity
{
    /// <summary>
    /// Low impact (e.g., sequence gaps without balance mismatch).
    /// </summary>
    Low = 0,

    /// <summary>
    /// Medium impact (e.g., imbalanced transactions for a specific entity).
    /// </summary>
    Medium = 1,

    /// <summary>
    /// Critical impact (e.g., Global Invariant violated: Σ Debits != Σ Credits).
    /// </summary>
    Critical = 2
}
