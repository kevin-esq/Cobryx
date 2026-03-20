using Cobryx.Domain.ML;

namespace Cobryx.Application.ML.Simulation;

public class RegimeEngine(int seed)
{
    private readonly Random _rng = new(seed);

    public MarketRegime Current { get; private set; } = MarketRegime.Normal;

    public MarketRegime Next()
    {
        var p = _rng.NextDouble();

        Current = Current switch
        {
            MarketRegime.Normal => p < 0.1 ? MarketRegime.HighInflation : MarketRegime.Normal,
            MarketRegime.HighInflation => p < 0.2 ? MarketRegime.Crisis : MarketRegime.HighInflation,
            MarketRegime.Crisis => p < 0.3 ? MarketRegime.Recovery : MarketRegime.Crisis,
            MarketRegime.Recovery => p < 0.5 ? MarketRegime.Normal : MarketRegime.Recovery,
            _ => MarketRegime.Normal
        };

        return Current;
    }
}
