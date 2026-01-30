using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Events;
using Concordia;
using Microsoft.Extensions.Logging;
using Cobryx.Application.Common.Events;

namespace Cobryx.Application.Auth.Events;

public class NewDeviceLoginEventHandler : INotificationHandler<DomainEventNotification<NewDeviceLoginEvent>>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<NewDeviceLoginEventHandler> _logger;

    public NewDeviceLoginEventHandler(IEmailService emailService, ILogger<NewDeviceLoginEventHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<NewDeviceLoginEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        _logger.LogInformation("Security alert: New device login for user {Email}", domainEvent.User.Email);

        await _emailService.SendEmailAsync(
            domainEvent.User.Email.Value,
            "Alerta de seguridad: Nuevo inicio de sesión",
            $"Hola {domainEvent.User.FirstName}, se detectó un inicio de sesión desde un nuevo dispositivo o ubicación: \n\n" +
            $"IP: {domainEvent.IpAddress}\n" +
            $"Navegador/Dispositivo: {domainEvent.UserAgent}\n\n" +
            "Si no fuiste tú, por favor cambia tu contraseña inmediatamente.",
            cancellationToken);
    }
}
