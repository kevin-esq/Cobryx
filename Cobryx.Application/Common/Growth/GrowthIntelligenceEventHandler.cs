using System.Threading;
using System.Threading.Tasks;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Events.Lending;
using Cobryx.Domain.Events.Payments;
using Cobryx.Domain.Events.Onboarding;
using Concordia;
using Cobryx.Application.Common.Events;

namespace Cobryx.Application.Common.Growth;

/// <summary>
/// Unifies domain events into specific Growth and Conversion signals.
/// Bridges the gap between core business actions and persistent growth intelligence.
/// </summary>
public class GrowthIntelligenceEventHandler :
    INotificationHandler<DomainEventNotification<LoanCreatedEvent>>,
    INotificationHandler<DomainEventNotification<PaymentCompletedEvent>>,
    INotificationHandler<DomainEventNotification<OnboardingMilestoneReachedEvent>>
{
    private readonly IGrowthIntelligenceService _growthService;

    public GrowthIntelligenceEventHandler(IGrowthIntelligenceService growthService)
    {
        _growthService = growthService;
    }

    public async Task Handle(DomainEventNotification<LoanCreatedEvent> notification, CancellationToken cancellationToken)
    {
        await _growthService.RecordWowAsync(notification.DomainEvent.TenantId, "LOAN.CREATED");
        _growthService.RecordFeatureActivation(notification.DomainEvent.TenantId, "LENDING.LOAN_MANAGEMENT");
    }

    public async Task Handle(DomainEventNotification<PaymentCompletedEvent> notification, CancellationToken cancellationToken)
    {
        await _growthService.RecordWowAsync(notification.DomainEvent.TenantId, "PAYMENT.COLLECTED");
        _growthService.RecordFeatureActivation(notification.DomainEvent.TenantId, "FINANCE.PAYMENT_PROCESSING");
    }

    public async Task Handle(DomainEventNotification<OnboardingMilestoneReachedEvent> notification, CancellationToken cancellationToken)
    {
        if (notification.DomainEvent.MilestoneCode == "ONBOARDING.COMPLETED")
        {
            await _growthService.MarkOnboardingCompletedAsync(notification.DomainEvent.TenantId);
        }

        // Track milestone activation heatmap
        _growthService.RecordFeatureActivation(notification.DomainEvent.TenantId, notification.DomainEvent.MilestoneCode);
    }
}
