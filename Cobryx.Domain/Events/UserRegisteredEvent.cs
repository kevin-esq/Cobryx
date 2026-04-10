using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Events;

public record UserRegisteredEvent(Guid UserId, Guid TenantId, string Email, string FirstName, DateTime? OccurredOnOverride = null) : IDomainEvent
{
    public DateTime OccurredOn { get; } = OccurredOnOverride ?? DateTime.UtcNow;
}
