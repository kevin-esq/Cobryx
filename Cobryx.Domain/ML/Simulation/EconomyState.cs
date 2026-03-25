namespace Cobryx.Domain.ML.Simulation;

public class EconomyState
{
    public decimal AvgPd { get; set; }
    public decimal Exposure { get; set; }
    public decimal Liquidity { get; set; }

    public decimal Inflation { get; set; }
    public decimal InterestRate { get; set; }
    public decimal Unemployment { get; set; }

    public MarketRegime Regime { get; set; }
}
