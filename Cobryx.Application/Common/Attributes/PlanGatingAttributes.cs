using Cobryx.Application.Subscriptions.Common;

namespace Cobryx.Application.Common.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequiresFeatureAttribute : Attribute
{
    public PlanFeature Feature { get; }

    public RequiresFeatureAttribute(PlanFeature feature)
    {
        Feature = feature;
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequiresLimitAttribute : Attribute
{
    public PlanLimitType LimitType { get; }

    public RequiresLimitAttribute(PlanLimitType limitType)
    {
        LimitType = limitType;
    }
}
