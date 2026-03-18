namespace Cobryx.Domain.Analytics;

public class LoanBalanceSnapshot
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid LoanId { get; private set; }
    public SnapshotType Type { get; private set; }

    public decimal PrincipalBalance { get; private set; }
    public decimal InterestBalance { get; private set; }
    public decimal LateFeeBalance { get; private set; }
    public int DaysPastDue { get; private set; }
    public long LedgerSequenceId { get; private set; }
    public DateTime RecordedAt { get; private set; }

    public decimal Outstanding => PrincipalBalance + InterestBalance + LateFeeBalance;

    private LoanBalanceSnapshot() { }

    public LoanBalanceSnapshot(
        Guid tenantId,
        Guid loanId,
        SnapshotType type,
        decimal principal,
        decimal interest,
        decimal lateFees,
        int daysPastDue,
        long ledgerSequenceId,
        DateTime recordedAt)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        LoanId = loanId;
        Type = type;

        PrincipalBalance = principal;
        InterestBalance = interest;
        LateFeeBalance = lateFees;

        DaysPastDue = daysPastDue;
        LedgerSequenceId = ledgerSequenceId;
        RecordedAt = recordedAt;
    }
}
