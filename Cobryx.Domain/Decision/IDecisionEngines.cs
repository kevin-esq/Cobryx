namespace Cobryx.Domain.Decision;

public interface ICreditLimitEngine
{
    decimal Calculate(CreditContext context);
}

public interface IPricingEngine
{
    decimal CalculateRate(PricingContext context);
}

public interface IFraudEngine
{
    decimal CalculateScore(FraudContext context);
}
