using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;

namespace Cobryx.Application.Tenants.Commands.ManageSubscription;

public record UpgradeSubscriptionCommand(Guid NewPlanId) : IRequest<Result>;

public class UpgradeSubscriptionHandler : IRequestHandler<UpgradeSubscriptionCommand, Result>
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUsageMeteringService _usageMetering;
    private readonly ITenantSubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionPlanRepository _planRepository;

    public UpgradeSubscriptionHandler(
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        IUsageMeteringService usageMetering,
        ITenantSubscriptionRepository subscriptionRepository,
        ISubscriptionPlanRepository planRepository)
    {
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
        _usageMetering = usageMetering;
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
    }

    public async Task<Result> Handle(UpgradeSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure(DomainErrorCode.Tenant.ContextMissing);

        var subscription = await _subscriptionRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken);

        if (subscription == null) return Result.Failure(DomainErrorCode.Subscription.NotFound);

        var newPlan = await _planRepository.GetByIdAsync(request.NewPlanId);
        if (newPlan == null) return Result.Failure(DomainErrorCode.Subscription.PlanNotFound);

        // Validate plan capacity (Downgrade rejection logic)
        var usage = await _usageMetering.GetUsageSnapshotAsync(tenantId.Value, cancellationToken);
        if (usage.InvoicesCount > newPlan.MaxInvoices)
            return Result.Failure(DomainErrorCode.Subscription.DowngradeNotAllowed);

        if (usage.ActiveUsersCount > newPlan.MaxUsers)
            return Result.Failure(DomainErrorCode.Subscription.DowngradeNotAllowed);

        subscription.UpdatePlan(newPlan);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
