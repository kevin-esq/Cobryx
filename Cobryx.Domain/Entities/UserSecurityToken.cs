using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;

namespace Cobryx.Domain.Entities;

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
        if (!IsActive) throw new InvalidOperationException("Token is not active.");
        UsedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void Revoke()
    {
        IsRevoked = true;
        UpdateTimestamp();
    }
}
