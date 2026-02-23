using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Subscriptions.Common;
using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Subscriptions.Queries.GetSubscriptionIntelligence;

public record GetSubscriptionIntelligenceQuery : IRequest<Result<SubscriptionIntelligenceDto>>;

public class GetSubscriptionIntelligenceHandler : IRequestHandler<GetSubscriptionIntelligenceQuery, Result<SubscriptionIntelligenceDto>>
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IUsageMeteringService _usageMeteringService;
    private readonly ITenantSubscriptionRepository _subscriptionRepository;

    public GetSubscriptionIntelligenceHandler(
        ITenantProvider tenantProvider,
        IUsageMeteringService usageMeteringService,
        ITenantSubscriptionRepository subscriptionRepository)
    {
        _tenantProvider = tenantProvider;
        _usageMeteringService = usageMeteringService;
        _subscriptionRepository = subscriptionRepository;
    }

    public async Task<Result<SubscriptionIntelligenceDto>> Handle(GetSubscriptionIntelligenceQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<SubscriptionIntelligenceDto>(DomainErrorCode.Tenant.ContextMissing);

        var usage = await _usageMeteringService.GetUsageSnapshotAsync(tenantId.Value, cancellationToken);
        var subscription = await _subscriptionRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken);

        if (subscription == null) return Result.Failure<SubscriptionIntelligenceDto>(DomainErrorCode.Subscription.NotFound);

        var usageMetrics = new Dictionary<string, SubscriptionResourceUsageDto>
        {
            ["Invoices"] = MapToResourceUsage(usage.InvoicesCount, usage.MaxInvoices),
            ["Users"] = MapToResourceUsage(usage.ActiveUsersCount, usage.MaxUsers),
            ["Loans"] = MapToResourceUsage(usage.ActiveLoansCount, usage.MaxLoans)
        };

        var capabilities = MapTierToCapabilities(subscription.Plan.Tier);

        var isNearAnyLimit = usageMetrics.Values.Any(u => u.IsNearLimit);
        var isPastDue = subscription.Status == SubscriptionStatus.PastDue;

        // Upgrade recommended if near limits, past due, or on a low tier (simplified)
        var upgradeRecommended = isNearAnyLimit || isPastDue || subscription.Plan.Tier == PlanTier.Free;

        return Result.Success(new SubscriptionIntelligenceDto(
            subscription.Status.ToString(),
            subscription.Plan.Name,
            subscription.Plan.Tier.ToString(),
            usageMetrics,
            capabilities,
            upgradeRecommended
        ));
    }

    private static SubscriptionResourceUsageDto MapToResourceUsage(int used, int limit)
    {
        var remaining = Math.Max(0, limit - used);
        var isNearLimit = limit > 0 && (double)used / limit >= 0.8;

        return new SubscriptionResourceUsageDto(used, limit, remaining, isNearLimit);
    }

    private static PlanCapabilitiesDto MapTierToCapabilities(PlanTier tier) => tier switch
    {
        PlanTier.Free => new PlanCapabilitiesDto(Lending: false, AdvancedReporting: false, WhiteLabeling: false),
        PlanTier.Starter => new PlanCapabilitiesDto(Lending: true, AdvancedReporting: false, WhiteLabeling: false),
        PlanTier.Pro => new PlanCapabilitiesDto(Lending: true, AdvancedReporting: true, WhiteLabeling: false),
        PlanTier.Business => new PlanCapabilitiesDto(Lending: true, AdvancedReporting: true, WhiteLabeling: true),
        _ => new PlanCapabilitiesDto(false, false, false)
    };
}
