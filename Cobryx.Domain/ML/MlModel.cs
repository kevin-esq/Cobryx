namespace Cobryx.Domain.ML;

public class MlModel
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Version { get; private set; } = string.Empty;
    public bool IsProduction { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private MlModel() { }

    public MlModel(string version, bool isProduction = false, DateTime? now = null)
    {
        Version = version;
        IsProduction = isProduction;
        CreatedAt = now ?? DateTime.UtcNow;
    }

    public void SetProduction(bool isProduction) => IsProduction = isProduction;
}
