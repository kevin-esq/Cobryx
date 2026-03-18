namespace Cobryx.Domain.ML;

public class ShadowPrediction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerId { get; set; }

    public decimal ProductionPd { get; set; }
    public decimal ShadowPd { get; set; }

    public string ProductionModelVersion { get; set; } = string.Empty;
    public string ShadowModelVersion { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
