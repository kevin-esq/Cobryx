namespace Cobryx.Domain.ML;

public class ShadowPrediction
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid CustomerId { get; private set; }
    public decimal ProductionPd { get; private set; }
    public decimal ShadowPd { get; private set; }
    public string ProductionModelVersion { get; private set; } = string.Empty;
    public string ShadowModelVersion { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private ShadowPrediction() { }

    public ShadowPrediction(
        Guid customerId,
        decimal productionPd,
        decimal shadowPd,
        string productionModelVersion,
        string shadowModelVersion,
        DateTime? now = null)
    {
        CustomerId = customerId;
        ProductionPd = productionPd;
        ShadowPd = shadowPd;
        ProductionModelVersion = productionModelVersion;
        ShadowModelVersion = shadowModelVersion;
        CreatedAt = now ?? DateTime.UtcNow;
    }
}
