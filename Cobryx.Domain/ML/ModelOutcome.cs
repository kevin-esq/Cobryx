namespace Cobryx.Domain.ML;

public class ModelOutcome
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }

    public decimal PredictedPD { get; private set; }
    public bool Defaulted { get; private set; }
    public decimal AmountRecovered { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private ModelOutcome() { }

    public ModelOutcome(Guid customerId, decimal pd, bool defaulted, decimal recovered)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        PredictedPD = pd;
        Defaulted = defaulted;
        AmountRecovered = recovered;
        CreatedAt = DateTime.UtcNow;
    }
}
