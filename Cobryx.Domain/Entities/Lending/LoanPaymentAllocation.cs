using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities.Lending;

public class LoanPaymentAllocation : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid LoanId { get; private set; }
    public Guid PaymentId { get; private set; }

    public decimal PrincipalApplied { get; private set; }
    public decimal InterestApplied { get; private set; }
    public decimal FeesApplied { get; private set; }
    public decimal UnappliedAmount { get; private set; }

    public long SnapshotSequence { get; private set; }

    private LoanPaymentAllocation() { }

    public LoanPaymentAllocation(
        Guid tenantId,
        Guid loanId,
        Guid paymentId,
        decimal principalApplied,
        decimal interestApplied,
        decimal feesApplied,
        decimal unappliedAmount,
        long snapshotSequence)
    {
        TenantId = tenantId;
        LoanId = loanId;
        PaymentId = paymentId;
        PrincipalApplied = principalApplied;
        InterestApplied = interestApplied;
        FeesApplied = feesApplied;
        UnappliedAmount = unappliedAmount;
        SnapshotSequence = snapshotSequence;
    }
}
