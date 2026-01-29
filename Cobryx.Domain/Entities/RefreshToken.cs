using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string Token { get; private set; }
    public DateTime Expires { get; private set; }
    public DateTime Created { get; private set; }
    public string CreatedByIp { get; private set; }
    public DateTime? Revoked { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByToken { get; private set; }
    public Guid UserId { get; private set; }
    public Guid SessionId { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= Expires;
    public bool IsRevoked => Revoked != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken()
    {
        Token = null!;
        CreatedByIp = null!;
    }

    public RefreshToken(string token, DateTime expires, string createdByIp, Guid userId, Guid sessionId)
    {
        Token = token;
        Expires = expires;
        Created = DateTime.UtcNow;
        CreatedByIp = createdByIp;
        UserId = userId;
        SessionId = sessionId;
    }

    public void Revoke(string ipAddress, string? replacedByToken = null)
    {
        Revoked = DateTime.UtcNow;
        RevokedByIp = ipAddress;
        ReplacedByToken = replacedByToken;
    }
}
