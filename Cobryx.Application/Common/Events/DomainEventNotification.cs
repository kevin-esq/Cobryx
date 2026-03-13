using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Common.Events;

public class DomainEventNotification<T> : INotification where T : IDomainEvent
{
    public T DomainEvent { get; }

    public DomainEventNotification(T domainEvent)
    {
        DomainEvent = domainEvent;
    }
}
