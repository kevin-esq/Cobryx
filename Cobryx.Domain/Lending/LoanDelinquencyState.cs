using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;
namespace Cobryx.Domain.Lending;

/// <summary>
/// Materialized delinquency state for a loan.
/// Updated daily by the Collections Engine.
/// </summary>
public class LoanDelinquencyState : BaseEntity
{
    public Guid LoanId { get; private set; }
    public int DaysPastDue { get; private set; }
    public DateTime? OldestUnpaidDueDate { get; private set; }
    public DelinquencyStage Stage { get; private set; }
    public DateTime LastEvaluatedAt { get; private set; }
    public DateTime? LastEvaluatedDate { get; private set; } // Daily watermark

    // Navigation
    public virtual Loan Loan { get; private set; } = null!;

    private LoanDelinquencyState() { }

    public LoanDelinquencyState(Guid loanId)
    {
        LoanId = loanId;
        Stage = DelinquencyStage.Current;
        DaysPastDue = 0;
        LastEvaluatedAt = DateTime.UtcNow;
    }

    public void UpdateState(int dpd, DateTime? oldestDueDate, DelinquencyStage stage, DateTime evaluatedAt, DateTime evaluatedDate)
    {
        DaysPastDue = dpd;
        OldestUnpaidDueDate = oldestDueDate;
        Stage = stage;
        LastEvaluatedAt = evaluatedAt;
        LastEvaluatedDate = evaluatedDate;
        UpdateTimestamp();
    }
}
