namespace Cobryx.Domain.Analytics;

public class TenantPortfolioAggregate
{
    public Guid TenantId { get; set; }
    public int TotalLoans { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal TotalPrincipal { get; set; }
    public decimal TotalInterest { get; set; }
    public decimal TotalLateFees { get; set; }
    public decimal NplOutstanding { get; set; }
    public decimal Bucket0To30 { get; set; }
    public decimal Bucket31To60 { get; set; }
    public decimal Bucket61To90 { get; set; }
    public decimal Bucket90Plus { get; set; }
}
