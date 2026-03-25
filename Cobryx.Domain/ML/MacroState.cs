namespace Cobryx.Domain.ML;

public class MacroState
{
    public decimal InterestRate { get; set; }
    public decimal Inflation { get; set; }
    public decimal Unemployment { get; set; }

    public decimal CreditSpread { get; set; }
    public decimal MarketVolatility { get; set; }

    public decimal LiquidityIndex { get; set; }

    public MarketRegime Regime { get; set; } = MarketRegime.Normal;
    public string Country { get; set; } = "US";

    public decimal InflationTMinus1 { get; set; }
    public decimal InflationTMinus2 { get; set; }
    public decimal RateTrend { get; set; }
}
