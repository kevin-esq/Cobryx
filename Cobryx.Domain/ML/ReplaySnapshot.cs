namespace Cobryx.Domain.ML;

public class ReplaySnapshot
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime OriginalTimestamp { get; set; }

    public string FeatureVectorJson { get; set; } = "";
    public string MacroStateJson { get; set; } = "";
    public string PortfolioStateJson { get; set; } = "";

    public decimal OriginalCreditLimit { get; set; }
    public decimal OriginalInterestRate { get; set; }

    public decimal? ReplayedCreditLimit { get; set; }
    public decimal? ReplayedInterestRate { get; set; }

    public string ModelVersion { get; set; } = "";
    public string ReplayModelVersion { get; set; } = "";

    public decimal DeltaCredit { get; set; }
    public decimal DeltaInterest { get; set; }

    // GAP 1: Determinism
    public int RandomSeed { get; set; }

    // GAP 2: Versioning
    public string ModelHash { get; set; } = "";
    public string FeatureVersion { get; set; } = "1.0";
    public string ScenarioVersion { get; set; } = "1.0";

    // GAP 3: True Reality Outcomes
    public bool? RealOutcomeDefaulted { get; set; }
    public decimal? RealOutcomeRecovered { get; set; }
    public decimal? RealOutcomeProfit { get; set; }
}
