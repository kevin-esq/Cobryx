using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Events;

public class NewDeviceLoginEvent : IDomainEvent
{
    public Guid UserId { get; }
    public string Email { get; }
    public string IpAddress { get; }
    public string UserAgent { get; }
    public DateTime OccurredOn { get; }

    public NewDeviceLoginEvent(Guid userId, string email, string ipAddress, string userAgent)
    {
        UserId = userId;
        Email = email;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        OccurredOn = DateTime.UtcNow;
    }
}
