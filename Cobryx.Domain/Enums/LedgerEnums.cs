namespace Cobryx.Domain.Enums;

/// <summary>
/// Standard accounting account types according to double-entry principles.
/// </summary>
public enum LedgerAccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense
}

/// <summary>
/// Lifecycle status of a journal transaction.
/// </summary>
public enum LedgerTransactionStatus
{
    Draft,
    Posted,
    Reversed
}

/// <summary>
/// Strategy for distributing income across different loan components.
/// </summary>
public enum PaymentAllocationPolicy
{
    /// <summary>
    /// Default: Pay off interest first, then penalties, then principal.
    /// </summary>
    InterestFirst,

    /// <summary>
    /// Pay off principal first (rare in consumer lending, but common in some corporate deals).
    /// </summary>
    PrincipalFirst,

    /// <summary>
    /// Distribute proportionally across all pending components.
    /// </summary>
    ProRata
}
