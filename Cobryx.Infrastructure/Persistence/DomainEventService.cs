using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Events;
using Cobryx.Domain.Common;
using Concordia;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Persistence;

public class DomainEventService : IDomainEventService
{
    private readonly ILogger<DomainEventService> _logger;
    private readonly IMediator _mediator;

    public DomainEventService(ILogger<DomainEventService> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Publish(IDomainEvent domainEvent)
    {
        _logger.LogInformation("Publishing domain event: {Event}", domainEvent.GetType().Name);

        var notification = CreateNotification(domainEvent);
        await _mediator.Publish(notification);
    }

    private static INotification CreateNotification(IDomainEvent domainEvent)
    {
        var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
        return (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
    }
}
