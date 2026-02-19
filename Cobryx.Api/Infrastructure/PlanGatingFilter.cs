using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Subscriptions.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cobryx.Api.Infrastructure;

/// <summary>
/// Global filter that enforces Plan Intelligence gating via [RequiresFeature] and [RequiresLimit].
/// </summary>
public class PlanGatingFilter : IAsyncActionFilter
{
    private readonly ISubscriptionEnforcementService _enforcementService;
    private readonly ITenantSubscriptionRepository _subscriptionRepository;
    private readonly ITenantProvider _tenantProvider;

    public PlanGatingFilter(
        ISubscriptionEnforcementService enforcementService,
        ITenantSubscriptionRepository subscriptionRepository,
        ITenantProvider tenantProvider)
    {
        _enforcementService = enforcementService;
        _subscriptionRepository = subscriptionRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
        {
            await next();
            return;
        }

        // 1. Feature Check
        var featureAttr = context.ActionDescriptor.EndpointMetadata.OfType<RequiresFeatureAttribute>().FirstOrDefault();
        if (featureAttr != null)
        {
            var subscription = await _subscriptionRepository.GetByTenantIdAsync(tenantId.Value, context.HttpContext.RequestAborted);
            if (subscription == null || !IsFeatureSupported(subscription.Plan.Tier, featureAttr.Feature))
            {
                context.Result = new ObjectResult(new { error = $"Plan Upgrade Required: Feature '{featureAttr.Feature}' is not available on your current plan." })
                {
                    StatusCode = 403
                };
                return;
            }
        }

        // 2. Limit Check (Using Enforcement Service with DB Locks for consistency)
        var limitAttr = context.ActionDescriptor.EndpointMetadata.OfType<RequiresLimitAttribute>().FirstOrDefault();
        if (limitAttr != null)
        {
            try
            {
                switch (limitAttr.LimitType)
                {
                    case PlanLimitType.Invoices:
                        await _enforcementService.EnsureWithinInvoicesLimitAsync(tenantId.Value, context.HttpContext.RequestAborted);
                        break;
                    case PlanLimitType.Users:
                        await _enforcementService.EnsureWithinUsersLimitAsync(tenantId.Value, context.HttpContext.RequestAborted);
                        break;
                    case PlanLimitType.Loans:
                        await _enforcementService.EnsureWithinLoansLimitAsync(tenantId.Value, context.HttpContext.RequestAborted);
                        break;
                }
            }
            catch (Exception ex) when (ex.GetType().Name.Contains("SubscriptionLimitExceededException"))
            {
                context.Result = new ObjectResult(new { error = ex.Message })
                {
                    StatusCode = 402 // Payment Required - Standard for billing limits
                };
                return;
            }
        }

        await next();
    }

    private static bool IsFeatureSupported(PlanTier tier, PlanFeature feature) => feature switch
    {
        PlanFeature.Lending => tier >= PlanTier.Starter,
        PlanFeature.AdvancedReporting => tier >= PlanTier.Pro,
        PlanFeature.WhiteLabeling => tier >= PlanTier.Business,
        _ => false
    };
}
