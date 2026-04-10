using Cobryx.Application.Common.Events;
using Cobryx.Domain.Events.Onboarding;
using Cobryx.Domain.Identity.Metrics;
using Cobryx.Domain.Interfaces;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Dashboard.EventHandlers;

public class OnboardingTelemetryHandler : INotificationHandler<DomainEventNotification<OnboardingMilestoneReachedEvent>>
{
    private readonly IUnitOfWork _unitOfWork;

    public OnboardingTelemetryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DomainEventNotification<OnboardingMilestoneReachedEvent> notification, CancellationToken cancellationToken)
    {
        var dbContext = (DbContext)_unitOfWork;
        var @event = notification.DomainEvent;

        var exists = await dbContext.Set<TenantFunnelMetric>()
            .AnyAsync(m => m.TenantId == @event.TenantId && m.MilestoneCode == @event.MilestoneCode, cancellationToken);

        if (exists)
            return;

        var previous = await dbContext.Set<TenantFunnelMetric>()
            .Where(m => m.TenantId == @event.TenantId)
            .OrderByDescending(m => m.ReachedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        double? deltaSeconds = null;
        if (previous != null)
        {
            deltaSeconds = (@event.ReachedAtUtc - previous.ReachedAtUtc).TotalSeconds;
        }

        var metric = new TenantFunnelMetric(
            @event.TenantId,
            @event.MilestoneCode,
            @event.ReachedAtUtc,
            deltaSeconds);

        dbContext.Set<TenantFunnelMetric>().Add(metric);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
