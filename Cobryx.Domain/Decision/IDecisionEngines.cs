namespace Cobryx.Domain.Decision;

public interface ICreditLimitEngine
{
    public decimal Calculate(CreditContext context);
}

public interface IPricingEngine
{
    public decimal CalculateRate(PricingContext context);
}

public interface IFraudEngine
{
    public decimal CalculateScore(FraudContext context);
}
