using Cobryx.Domain.Config;

namespace Cobryx.Application.ML;

public class GuardrailEngine
{
    public (decimal creditLimit, decimal interestRate) Apply(
        decimal creditLimit,
        decimal interestRate,
        Cobryx.Domain.ML.PortfolioState globalState)
    {
        if (globalState.TotalExposure > RiskLimits.MaxPortfolioExposure)
            creditLimit = 0m;

        if (globalState.AvailableLiquidity < RiskLimits.MinLiquidityThreshold)
            creditLimit *= 0.1m;

        creditLimit = Math.Clamp(creditLimit, 0m, RiskLimits.MaxCreditLimit);
        interestRate = Math.Clamp(interestRate, RiskLimits.MinInterestRate, RiskLimits.MaxInterestRate);

        return (creditLimit, interestRate);
    }
}
