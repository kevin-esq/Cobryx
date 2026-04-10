using Cobryx.Application.Common.Interfaces;

using Microsoft.Extensions.Configuration;

namespace Cobryx.Infrastructure.Services;

public class FeatureFlagsService(IConfiguration configuration) : IFeatureFlags
{
    public bool IsMlScoringEnabled =>
        configuration.GetValue("FeatureFlags:ML_SCORING_ENABLED", true);

    public bool IsPaymentsEnabled =>
        configuration.GetValue("FeatureFlags:PAYMENTS_ENABLED", true);

    public bool IsWebhooksEnabled =>
        configuration.GetValue("FeatureFlags:WEBHOOKS_ENABLED", true);
}
