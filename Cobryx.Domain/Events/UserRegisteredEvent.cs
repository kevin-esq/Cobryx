using System;
using Cobryx.Domain.Common;

namespace Cobryx.Domain.Events;

public record UserRegisteredEvent(Guid UserId, Guid TenantId, string Email, string FirstName) : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
