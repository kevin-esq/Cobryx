namespace Cobryx.Application.Common.Interfaces;

public interface IRandomProvider
{
    public double NextDouble();
    public int Next();
    public int Next(int maxValue);
    public int Next(int minValue, int maxValue);

    public void Reseed(int seed);
}
