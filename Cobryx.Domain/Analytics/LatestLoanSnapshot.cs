namespace Cobryx.Domain.Analytics;

public class LatestLoanSnapshot
{
    public Guid LoanId { get; set; }
    public decimal PrincipalBalance { get; set; }
    public decimal InterestBalance { get; set; }
    public decimal LateFeeBalance { get; set; }
    public int DaysPastDue { get; set; }

    public decimal Outstanding => PrincipalBalance + InterestBalance + LateFeeBalance;
}
