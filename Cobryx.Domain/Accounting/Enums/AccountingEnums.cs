namespace Cobryx.Domain.Accounting.Enums;

/// <summary>
/// Standard accounting account types according to double-entry principles.
/// </summary>
public enum LedgerAccountType { Asset, Liability, Equity, Revenue, Expense }

/// <summary>
/// Operational roles for ledger accounts, enabling Stripe-grade
/// granular tracking (Available, Pending, Reserve, etc).
/// </summary>
public enum LedgerAccountRole
{
    None = 0,
    Available = 1,
    Pending = 2,
    Reserve = 3,
    Receivable = 4,
    Loss = 5,
    Fees = 6,
    Treasury = 7,
    Settlement = 8
}

/// <summary>
/// Lifecycle status of a journal transaction.
/// </summary>
public enum LedgerTransactionStatus { Draft, Posted, Reversed }

/// <summary>
/// Strategy for distributing income across different loan components.
/// </summary>
public enum PaymentAllocationPolicy { InterestFirst, PrincipalFirst, ProRata }
