using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Events;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Events;
using Cobryx.Domain.Interfaces;

using Concordia;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cobryx.Application.Auth.Commands.Register;

public class UserRegisteredEventHandler : INotificationHandler<DomainEventNotification<UserRegisteredEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserRegisteredEventHandler> _logger;
    private readonly AppOptions _appOptions;

    public UserRegisteredEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        ILogger<UserRegisteredEventHandler> logger,
        IOptions<AppOptions> appOptions)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _logger = logger;
        _appOptions = appOptions.Value;
    }

    public async Task Handle(DomainEventNotification<UserRegisteredEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        _logger.LogInformation("Handling UserRegisteredEvent for User: {UserId}", domainEvent.UserId);

        var user = await _userRepository.GetByIdAsync(domainEvent.UserId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found when handling registration event.", domainEvent.UserId);
            return;
        }
    }
}
