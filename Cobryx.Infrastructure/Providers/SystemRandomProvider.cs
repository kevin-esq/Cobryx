using Cobryx.Application.Common.Interfaces;

namespace Cobryx.Infrastructure.Providers;

public class SystemRandomProvider : IRandomProvider
{
    private Random _rng;

    public SystemRandomProvider()
    {
        _rng = new Random();
    }

    public void Reseed(int seed)
    {
        _rng = new Random(seed);
    }

    public double NextDouble() => _rng.NextDouble();
    public int Next() => _rng.Next();
    public int Next(int maxValue) => _rng.Next(maxValue);
    public int Next(int minValue, int maxValue) => _rng.Next(minValue, maxValue);
}
