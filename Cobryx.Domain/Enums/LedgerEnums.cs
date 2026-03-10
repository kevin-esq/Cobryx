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
/// Operational roles for ledger accounts, enabling Stripe-grade
/// granular tracking (Available, Pending, Reserve, etc).
/// </summary>
public enum LedgerAccountRole
{
    None = 0,
    Available = 1,      // Liquid funds ready for payout
    Pending = 2,        // In-flight funds (e.g., card authorized but not captured)
    Reserve = 3,        // Risk/Dispute buffer
    Receivable = 4,     // User debt to the platform
    Loss = 5,           // Platform write-off
    Fees = 6,           // Platform revenue (commissions)
    Treasury = 7,       // Platform internal cash
    Settlement = 8      // External processor settlement bridge
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
