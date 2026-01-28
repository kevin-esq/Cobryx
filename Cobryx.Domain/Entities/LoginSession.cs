using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public class LoginSession : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string IpAddress { get; private set; }
    public string? DeviceFingerprint { get; private set; }
    public DateTime LastActiveAt { get; private set; }
    public bool IsRevoked { get; private set; }

    // Navigation
    public virtual User User { get; private set; } = null!;

    private LoginSession() 
    {
        IpAddress = null!;
    }

    public LoginSession(Guid tenantId, Guid userId, string ipAddress, string? deviceFingerprint)
    {
        TenantId = tenantId;
        UserId = userId;
        IpAddress = ipAddress;
        DeviceFingerprint = deviceFingerprint;
        LastActiveAt = DateTime.UtcNow;
        IsRevoked = false;
    }

    public void UpdateActivity()
    {
        LastActiveAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Revoke()
    {
        IsRevoked = true;
        UpdateTimestamp();
    }
}
