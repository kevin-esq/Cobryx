using Cobryx.Domain.Common;

namespace Cobryx.Domain.Events.Onboarding;

public record OnboardingMilestoneReachedEvent(
    Guid TenantId,
    string MilestoneCode,
    DateTime ReachedAtUtc
) : IDomainEvent
{
    public DateTime OccurredOn => ReachedAtUtc;
}
