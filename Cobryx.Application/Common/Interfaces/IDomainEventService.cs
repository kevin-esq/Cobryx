using Cobryx.Domain.Common;

namespace Cobryx.Application.Common.Interfaces;

public interface IDomainEventService
{
    Task Publish(IDomainEvent domainEvent);
}
