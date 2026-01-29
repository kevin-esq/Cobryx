using System;
using System.Collections.Generic;
using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public class User : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string FullName => $"{FirstName} {LastName}";
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsMfaEnabled { get; private set; }
    public DateTime? MfaEnabledAt { get; private set; }

    public virtual UserProfile? Profile { get; private set; }
    private readonly List<LoginSession> _sessions = new();
    public IReadOnlyCollection<LoginSession> Sessions => _sessions.AsReadOnly();

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private readonly List<MfaDevice> _mfaDevices = new();
    public IReadOnlyCollection<MfaDevice> MfaDevices => _mfaDevices.AsReadOnly();

    private readonly List<RecoveryCode> _recoveryCodes = new();
    public IReadOnlyCollection<RecoveryCode> RecoveryCodes => _recoveryCodes.AsReadOnly();

    private User()
    {
        FirstName = null!;
        LastName = null!;
        Email = null!;
        PasswordHash = null!;
        Role = null!;
    }

    public User(Guid tenantId, string firstName, string lastName, string email, Guid roleId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("FirstName is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("LastName is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required.", nameof(email));
        if (roleId == Guid.Empty) throw new ArgumentException("RoleId is required.", nameof(roleId));

        TenantId = tenantId;
        FirstName = firstName;
        LastName = lastName;
        Email = email.ToLowerInvariant();
        RoleId = roleId;
        PasswordHash = null!;
        Role = null!;
        IsActive = true;
        IsMfaEnabled = false;
    }

    public void EnableMfa()
    {
        IsMfaEnabled = true;
        MfaEnabledAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void DisableMfa()
    {
        IsMfaEnabled = false;
        MfaEnabledAt = null;
        _mfaDevices.Clear();
        _recoveryCodes.Clear();
        UpdateTimestamp();
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

    public RefreshToken AddRefreshToken(string token, DateTime expires, string createdByIp, Guid sessionId)
    {
        var refreshToken = new RefreshToken(token, expires, createdByIp, Id, sessionId);
        _refreshTokens.Add(refreshToken);
        return refreshToken;
    }

    public LoginSession AddSession(string ipAddress, string? deviceFingerprint)
    {
        var session = new LoginSession(TenantId, Id, ipAddress, deviceFingerprint);
        _sessions.Add(session);
        return session;
    }

    public void RevokeSession(Guid sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session != null)
        {
            session.Revoke();

            foreach (var token in _refreshTokens.Where(t => t.SessionId == sessionId && t.IsActive))
            {
                token.Revoke("Session Revocation");
            }
        }
    }
    public void InvalidateTokenChain(string tokenValue, string ipAddress)
    {
        var token = _refreshTokens.FirstOrDefault(t => t.Token == tokenValue);
        if (token == null) return;

        // If the token is already revoked, it might be a reuse attack.
        RevokeSession(token.SessionId);
    }
    public void AddMfaDevice(MfaDevice device)
    {
        if (device == null) throw new ArgumentNullException(nameof(device));
        _mfaDevices.Add(device);
        UpdateTimestamp();
    }

    public void AddRecoveryCode(RecoveryCode code)
    {
        if (code == null) throw new ArgumentNullException(nameof(code));
        _recoveryCodes.Add(code);
        UpdateTimestamp();
    }

    public void CreateProfile(string? phoneNumber = null, string? avatarUrl = null)
    {
        if (Profile != null) return;
        Profile = new UserProfile(Id, phoneNumber, avatarUrl);
    }

    public void RemoveOldRefreshTokens(int ttlDays)
    {
        _refreshTokens.RemoveAll(x =>
            !x.IsActive &&
            x.CreatedAt.AddDays(ttlDays) <= DateTime.UtcNow);
    }

    public bool HasValidRefreshToken(string token)
    {
        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(token);
        return _refreshTokens.Any(x =>
            System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(x.Token),
                tokenBytes)
            && x.IsActive);
    }
}
