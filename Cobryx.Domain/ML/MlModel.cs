namespace Cobryx.Domain.ML;

public class MlModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Version { get; set; } = string.Empty;
    public bool IsProduction { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
