using Cobryx.Domain.Entities;
using Cobryx.Domain.Enums;

namespace Cobryx.Domain.Services;

public class UsageService
{
    public bool CanPerformAction(TenantSubscription subscription, MetricType metricType, decimal currentUsage)
    {
        if (subscription == null || subscription.Status != SubscriptionStatus.Active)
            return false;

        var plan = subscription.Plan;
        if (plan == null) return false;

        return metricType switch
        {
            MetricType.Invoices => currentUsage < plan.MaxInvoices,
            MetricType.Users => currentUsage < plan.MaxUsers,
            _ => true
        };
    }

    public UsageRecord CreateUsageRecord(Guid tenantId, MetricType metricType, decimal quantity)
    {
        return new UsageRecord(tenantId, metricType, quantity);
    }
}
