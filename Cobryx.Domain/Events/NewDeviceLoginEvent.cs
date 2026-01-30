using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Events;

public class NewDeviceLoginEvent : IDomainEvent
{
    public User User { get; }
    public string IpAddress { get; }
    public string UserAgent { get; }
    public DateTime OccurredOn { get; }

    public NewDeviceLoginEvent(User user, string ipAddress, string userAgent)
    {
        User = user;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        OccurredOn = DateTime.UtcNow;
    }
}
