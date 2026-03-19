namespace Cobryx.Domain.ML;

public class Experience
{
    public System.Guid Id { get; set; } = System.Guid.NewGuid();

    public System.Guid CustomerId { get; set; }

    public string StateJson { get; set; } = "";

    public decimal CreditMultiplier { get; set; }
    public decimal InterestDelta { get; set; }

    public decimal LogProb { get; set; }
    public decimal Value { get; set; }

    public decimal Reward { get; set; }

    public string NextStateJson { get; set; } = "";
}
