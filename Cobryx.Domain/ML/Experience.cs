namespace Cobryx.Domain.ML;

public class Experience
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }

    public string StateJson { get; set; } = "";

    public decimal CreditMultiplier { get; set; }
    public decimal InterestDelta { get; set; }

    public decimal LogProb { get; set; }
    public decimal Value { get; set; }

    public decimal Reward { get; set; }

    public string NextStateJson { get; set; } = "";
    public bool Done { get; set; }
    public string Source { get; set; } = "production";
}
