using Cobryx.Domain.Identity.Enums;
using Cobryx.Domain.Shared;


namespace Cobryx.Domain.Identity;

public class UserSecurityToken : BaseEntity
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public SecurityTokenType Type { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public bool IsRevoked { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && UsedAt == null && !IsExpired;

    public virtual User User { get; private set; } = null!;

    private UserSecurityToken()
    {
        TokenHash = null!;
    }

    public UserSecurityToken(Guid userId, string tokenHash, SecurityTokenType type, int expiryMinutes)
    {
        UserId = userId;
        TokenHash = tokenHash;
        Type = type;
        ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
        IsRevoked = false;
    }

    public void Use()
    {
        if (!IsActive)
            throw new DomainException(DomainErrorCode.Auth.TokenNotActive);
        UsedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Revoke()
    {
        IsRevoked = true;
        UpdateTimestamp();
    }
}
