namespace Cobryx.Domain.ML;

public class PortfolioState
{
    public decimal TotalCapital { get; set; }
    public decimal AvailableLiquidity { get; set; }
    public decimal TotalExposure { get; set; }
    public decimal AveragePd { get; set; }
    public decimal DefaultRate { get; set; }
    public int ActiveLoans { get; set; }
    public decimal RevenueYTD { get; set; }
}
