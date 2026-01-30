using System.Threading;
using System.Threading.Tasks;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Events;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.Extensions.Logging;

using Cobryx.Application.Common.Events;

namespace Cobryx.Application.Auth.Commands.Register;

public class UserRegisteredEventHandler : INotificationHandler<DomainEventNotification<UserRegisteredEvent>>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserRegisteredEventHandler> _logger;

    public UserRegisteredEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        ILogger<UserRegisteredEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<UserRegisteredEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        _logger.LogInformation("Handling UserRegisteredEvent for User: {UserId}", domainEvent.UserId);

        var user = await _userRepository.GetByIdAsync(domainEvent.UserId);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found when handling registration event.", domainEvent.UserId);
            return;
        }

        // Only send if not verified (which should be the case for new users)
        if (!user.IsEmailVerified)
        {
            var token = user.SecurityTokens
                .FirstOrDefault(t => t.Type == Cobryx.Domain.Enums.SecurityTokenType.EmailVerification && t.IsActive);

            if (token != null)
            {
                await _emailService.SendEmailAsync(
                    user.Email.Value,
                    "Verifica tu cuenta Cobryx",
                    $"Hola {user.FirstName}, por favor verifica tu cuenta haciendo clic aquí: https://app.cobryx.com/verify?token={token.Token}",
                    cancellationToken);
            }
        }
    }
}
