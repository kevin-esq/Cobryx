namespace Cobryx.Domain.Analytics;

public class PortfolioMetricsDaily
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public DateTime Date { get; private set; }
    
    public int TotalLoans { get; private set; }
    public decimal TotalOutstanding { get; private set; }
    public decimal TotalPrincipal { get; private set; }
    public decimal TotalInterest { get; private set; }
    public decimal TotalLateFees { get; private set; }
    
    public decimal NPLRatio { get; private set; }
    public decimal DelinquencyRate { get; private set; }
    public decimal CollectionEfficiency { get; private set; }
    
    public decimal RevenueMTD { get; private set; }
    public decimal RevenueYTD { get; private set; }
    
    public decimal Bucket0To30 { get; private set; }
    public decimal Bucket31To60 { get; private set; }
    public decimal Bucket61To90 { get; private set; }
    public decimal Bucket90Plus { get; private set; }
    
    public DateTime LastUpdated { get; private set; }

    private PortfolioMetricsDaily() { }

    public PortfolioMetricsDaily(Guid tenantId, DateTime date)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Date = date;
        LastUpdated = DateTime.UtcNow;
    }
}
