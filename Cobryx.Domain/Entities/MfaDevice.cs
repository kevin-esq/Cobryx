using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public class MfaDevice : BaseEntity
{
    public Guid UserId { get; private set; }
    public string DeviceName { get; private set; } = null!;
    public MfaDeviceType Type { get; private set; }
    public string Secret { get; private set; } = null!;
    public string? CredentialId { get; private set; }
    public string? PublicKey { get; private set; }
    public uint Counter { get; private set; }
    public DateTime? LastUsedAt { get; private set; }
    public bool IsVerified { get; private set; }

    public virtual User User { get; private set; } = null!;

    private MfaDevice() { }

    public MfaDevice(Guid userId, string deviceName, MfaDeviceType type, string secret, string? credentialId = null, string? publicKey = null)
    {
        UserId = userId;
        DeviceName = deviceName;
        Type = type;
        Secret = secret;
        CredentialId = credentialId;
        PublicKey = publicKey;
        IsVerified = false;
    }

    public void Verify()
    {
        IsVerified = true;
        LastUsedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void UpdateUsage(uint counter)
    {
        LastUsedAt = DateTime.UtcNow;
        Counter = counter;
        UpdateTimestamp();
    }
}

public enum MfaDeviceType
{
    Totp,
    Fido2
}
