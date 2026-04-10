namespace Cobryx.Domain.Config;

public static class RiskLimits
{
    public const decimal MaxPortfolioExposure = 10_000_000m;
    public const decimal MinLiquidityThreshold = 100_000m;

    public const decimal MaxCreditLimit = 200_000m;

    public const decimal MinInterestRate = 0.05m;
    public const decimal MaxInterestRate = 0.45m;
}
