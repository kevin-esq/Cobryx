using System;
using System.Collections.Generic;
using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public class User : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string FullName { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; }

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { }

    public User(Guid tenantId, string fullName, string email, Guid roleId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("FullName is required.", nameof(fullName));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
        if (roleId == Guid.Empty) throw new ArgumentException("RoleId is required.", nameof(roleId));

        TenantId = tenantId;
        FullName = fullName;
        Email = email.ToLowerInvariant();
        RoleId = roleId;
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) 
            throw new ArgumentException("Password hash cannot be empty.");
            
        PasswordHash = passwordHash;
    }

    public void UpdateRole(Guid roleId)
    {
        if (roleId == Guid.Empty) throw new ArgumentException("RoleId is required.");
        RoleId = roleId;
    }

    public void AddRefreshToken(string token, DateTime expires, string createdByIp)
    {
        var refreshToken = new RefreshToken(token, expires, createdByIp, Id);
        _refreshTokens.Add(refreshToken);
    }

    public void RemoveOldRefreshTokens(int ttlDays)
    {
        _refreshTokens.RemoveAll(x => 
            !x.IsActive && 
            x.Created.AddDays(ttlDays) <= DateTime.UtcNow);
    }

    public bool HasValidRefreshToken(string token)
    {
        return _refreshTokens.Any(x => x.Token == token && x.IsActive);
    }
}
