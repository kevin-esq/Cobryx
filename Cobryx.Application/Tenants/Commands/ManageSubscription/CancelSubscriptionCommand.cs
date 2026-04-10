using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.Shared;
using Cobryx.Domain.Shared.Enums;

using Concordia;

namespace Cobryx.Application.Tenants.Commands.ManageSubscription
{
    public record CancelSubscriptionCommand(
        CancellationReason Reason,
        string? Feedback = null) : IRequest<Result>;

    public class CancelSubscriptionHandler(
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork,
        ITenantSubscriptionRepository subscriptionRepository,
        IClock clock) : IRequestHandler<CancelSubscriptionCommand, Result>
    {
        private readonly ITenantProvider _tenantProvider = tenantProvider;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;
        private readonly ITenantSubscriptionRepository _subscriptionRepository = subscriptionRepository;
        private readonly IClock _clock = clock;

        public async Task<Result> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
        {
            var tenantId = _tenantProvider.GetTenantId();
            if (!tenantId.HasValue)
            {
                return Result.Failure(DomainErrorCode.Tenant.ContextMissing);
            }

            var subscription = await _subscriptionRepository.GetByTenantIdAsync(tenantId.Value, cancellationToken);

            if (subscription == null)
            {
                return Result.Failure(DomainErrorCode.Subscription.NotFound);
            }

            if (subscription.Status == SubscriptionStatus.Cancelled)
            {
                return Result.Failure(DomainErrorCode.Subscription.AlreadyCancelled);
            }

            var gracePeriodEnd = _clock.UtcNow.AddDays(7);

            subscription.ExecuteCancellation(gracePeriodEnd, _clock.UtcNow, request.Reason, request.Feedback);

            _ = await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
