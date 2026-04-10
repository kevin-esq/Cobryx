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

    public PortfolioMetricsDaily(Guid tenantId, DateTime date, DateTime? now = null)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Date = date;
        LastUpdated = now ?? DateTime.UtcNow;
    }

    public void SetTotals(int totalLoans, decimal totalOutstanding, decimal totalPrincipal, decimal totalInterest, decimal totalLateFees)
        => SetTotals(totalLoans, totalOutstanding, totalPrincipal, totalInterest, totalLateFees, DateTime.UtcNow);

    public void SetTotals(int totalLoans, decimal totalOutstanding, decimal totalPrincipal, decimal totalInterest, decimal totalLateFees, DateTime now)
    {
        TotalLoans = totalLoans;
        TotalOutstanding = totalOutstanding;
        TotalPrincipal = totalPrincipal;
        TotalInterest = totalInterest;
        TotalLateFees = totalLateFees;
        LastUpdated = now;
    }

    public void SetRisk(decimal nplRatio, decimal bucket0To30, decimal bucket31To60, decimal bucket61To90, decimal bucket90Plus)
        => SetRisk(nplRatio, bucket0To30, bucket31To60, bucket61To90, bucket90Plus, DateTime.UtcNow);

    public void SetRisk(decimal nplRatio, decimal bucket0To30, decimal bucket31To60, decimal bucket61To90, decimal bucket90Plus, DateTime now)
    {
        NPLRatio = nplRatio;
        Bucket0To30 = bucket0To30;
        Bucket31To60 = bucket31To60;
        Bucket61To90 = bucket61To90;
        Bucket90Plus = bucket90Plus;
        LastUpdated = now;
    }

    public void SetRevenue(decimal revenueMTD, decimal revenueYTD) => SetRevenue(revenueMTD, revenueYTD, DateTime.UtcNow);

    public void SetRevenue(decimal revenueMTD, decimal revenueYTD, DateTime now)
    {
        RevenueMTD = revenueMTD;
        RevenueYTD = revenueYTD;
        LastUpdated = now;
    }

    public void SetCollections(decimal collectionEfficiency) => SetCollections(collectionEfficiency, DateTime.UtcNow);

    public void SetCollections(decimal collectionEfficiency, DateTime now)
    {
        CollectionEfficiency = collectionEfficiency;
        LastUpdated = now;
    }
}
