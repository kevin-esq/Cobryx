using Cobryx.Domain.Shared;

namespace Cobryx.Application.Common.Interfaces;

public interface IDomainEventService
{
    public Task Publish(IDomainEvent domainEvent);
}
