using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Persistence;

public class DomainEventService : IDomainEventService
{
    private readonly ILogger<DomainEventService> _logger;

    public DomainEventService(ILogger<DomainEventService> logger)
    {
        _logger = logger;
    }

    public Task Publish(IDomainEvent domainEvent)
    {
        _logger.LogInformation("Publishing domain event: {Event}", domainEvent.GetType().Name);

        return Task.CompletedTask;
    }
}
