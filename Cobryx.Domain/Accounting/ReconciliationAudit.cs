using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Accounting;

/// <summary>
/// Record of a reconciliation run between Stripe and Internal Ledger.
/// Implements institutional audit trails with windowing and drift classification.
/// </summary>
public class ReconciliationAudit : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid RunId { get; private set; }

    public DateTime FromUtc { get; private set; }
    public DateTime ToUtc { get; private set; }
    public string? LastStripeCursor { get; private set; }

    public ReconciliationStatus Status { get; private set; }
    public ReconciliationSeverity Severity { get; private set; }

    public decimal LedgerBalance { get; private set; }
    public decimal StripeAvailableBalance { get; private set; }
    public decimal StripePendingBalance { get; private set; }
    public decimal Discrepancy => LedgerBalance - (StripeAvailableBalance + StripePendingBalance);

    public string? DriftDetailsJson { get; private set; }
    public int DetectedDriftsCount { get; private set; }
    public string? LedgerFingerprintSnapshot { get; private set; }

    private ReconciliationAudit() { }

    public ReconciliationAudit(
        Guid tenantId,
        Guid runId,
        DateTime fromUtc,
        DateTime toUtc,
        decimal ledgerBalance,
        decimal stripeAvailable,
        decimal stripePending,
        ReconciliationStatus status,
        ReconciliationSeverity severity,
        int driftsCount,
        string? detailsJson = null,
        string? fingerprint = null,
        string? lastCursor = null)
    {
        TenantId = tenantId;
        RunId = runId;
        FromUtc = fromUtc;
        ToUtc = toUtc;
        LedgerBalance = ledgerBalance;
        StripeAvailableBalance = stripeAvailable;
        StripePendingBalance = stripePending;
        Status = status;
        Severity = severity;
        DetectedDriftsCount = driftsCount;
        DriftDetailsJson = detailsJson;
        LedgerFingerprintSnapshot = fingerprint;
        LastStripeCursor = lastCursor;
    }
}
