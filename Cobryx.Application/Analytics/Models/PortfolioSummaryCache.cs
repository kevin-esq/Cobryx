namespace Cobryx.Application.Analytics.Models;

public class PortfolioSummaryCache
{
    public Guid TenantId { get; set; }
    public int TotalLoans { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal AverageLoanSize { get; set; }
    public decimal NplRatio { get; set; }
    public decimal DelinquencyRate { get; set; }
    public decimal RevenueMTD { get; set; }
    public decimal RevenueYTD { get; set; }
    public decimal CollectionEfficiency { get; set; }
    public decimal CashInflowMTD { get; set; }
    public decimal CashOutflowMTD { get; set; }
    public DateTime LastUpdated { get; set; }
}
