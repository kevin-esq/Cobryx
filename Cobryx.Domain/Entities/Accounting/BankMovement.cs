using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities.Accounting;

public class BankMovement : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = string.Empty;
    public BankMovementDirection Direction { get; private set; }
    public DateTime BookingDate { get; private set; }
    public DateTime ValueDate { get; private set; }

    public string Provider { get; private set; } = string.Empty; // Plaid, Swift, etc.
    public string ProviderTransactionId { get; private set; } = string.Empty;
    public string? ExternalRef { get; private set; }

    public BankMovementStatus Status { get; private set; }
    public Guid? MatchedLedgerTransactionId { get; private set; }
    public decimal? MatchingConfidence { get; private set; }
    public string? MatchingType { get; private set; } // Exact, Strong, Aggregate

    public string? RawPayload { get; private set; }

    private BankMovement() { } // EF Core

    public BankMovement(
        Guid tenantId,
        decimal amount,
        string currency,
        BankMovementDirection direction,
        DateTime bookingDate,
        DateTime valueDate,
        string provider,
        string providerTransactionId,
        string? externalRef = null,
        string? rawPayload = null)
    {
        TenantId = tenantId;
        Amount = Math.Abs(amount);
        Currency = currency.ToUpperInvariant();
        Direction = direction;
        BookingDate = bookingDate;
        ValueDate = valueDate;
        Provider = provider;
        ProviderTransactionId = providerTransactionId;
        ExternalRef = externalRef;
        RawPayload = rawPayload;
        Status = BankMovementStatus.Unmatched;
    }

    public void MarkAsMatched(Guid ledgerTransactionId, decimal confidence, string matchType)
    {
        MatchedLedgerTransactionId = ledgerTransactionId;
        MatchingConfidence = confidence;
        MatchingType = matchType;
        Status = BankMovementStatus.Matched;
        UpdateTimestamp();
    }

    public void MarkForInvestigation()
    {
        Status = BankMovementStatus.Investigating;
        UpdateTimestamp();
    }
}

public enum BankMovementDirection
{
    Inbound,
    Outbound
}

public enum BankMovementStatus
{
    Unmatched,
    Matched,
    Investigating
}
