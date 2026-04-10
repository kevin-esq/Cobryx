namespace Cobryx.Domain.Decision;

public class CreditContext
{
    public decimal ProbabilityOfDefault { get; set; }
    public decimal BehaviorScore { get; set; }
    public decimal MonthlyIncomeEstimate { get; set; }
    public decimal Utilization { get; set; }
}

public class PricingContext
{
    public decimal ProbabilityOfDefault { get; set; }
}

public class FraudContext
{
    public int TransactionsLastHour { get; set; }
    public decimal AmountVelocity { get; set; }
    public bool GeoAnomaly { get; set; }
}

public class DecisionContext
{
    public CreditContext Credit { get; set; } = default!;
    public PricingContext Pricing { get; set; } = default!;
    public FraudContext Fraud { get; set; } = default!;
}

public class DecisionResult
{
    public decimal CreditLimit { get; set; }
    public decimal InterestRate { get; set; }
    public decimal FraudScore { get; set; }
    public bool Approved { get; set; }

    public ExecutionTrace Trace { get; set; } = new();
}

public class CombinedDecisionResult
{
    public DecisionResult? Primary { get; set; }
    public DecisionResult? Shadow { get; set; }

    public bool HasDrift => Primary?.CreditLimit != Shadow?.CreditLimit ||
                            Primary?.InterestRate != Shadow?.InterestRate;
}
