namespace Cobryx.Application.Common.Interfaces;

public interface IFeatureFlags
{
    public bool IsMlScoringEnabled { get; }
    public bool IsPaymentsEnabled { get; }
    public bool IsWebhooksEnabled { get; }
}
