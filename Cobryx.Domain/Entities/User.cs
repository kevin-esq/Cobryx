using System;
using System.Collections.Generic;
using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Events;

namespace Cobryx.Domain.Entities;

public class User : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string FullName => $"{FirstName} {LastName}";
    public EmailAddress Email { get; private set; }
    public string PasswordHash { get; private set; }
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsMfaEnabled { get; private set; }
    public DateTime? MfaEnabledAt { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public LegalConsent? LegalConsent { get; private set; }
    public bool MarketingConsent { get; private set; }

    public virtual UserProfile? Profile { get; private set; }
    private readonly List<LoginSession> _sessions = new();
    public IReadOnlyCollection<LoginSession> Sessions => _sessions.AsReadOnly();

    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private readonly List<MfaDevice> _mfaDevices = new();
    public IReadOnlyCollection<MfaDevice> MfaDevices => _mfaDevices.AsReadOnly();

    private readonly List<RecoveryCode> _recoveryCodes = new();
    public IReadOnlyCollection<RecoveryCode> RecoveryCodes => _recoveryCodes.AsReadOnly();

    private readonly List<UserSecurityToken> _securityTokens = new();
    public IReadOnlyCollection<UserSecurityToken> SecurityTokens => _securityTokens.AsReadOnly();

    private User()
    {
        FirstName = null!;
        LastName = null!;
        Email = null!;
        PasswordHash = null!;
        Role = null!;
    }

    public User(Guid tenantId, string firstName, string lastName, EmailAddress email, Guid roleId)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("FirstName is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("LastName is required.", nameof(lastName));
        if (email == null) throw new ArgumentException("Email is required.", nameof(email));
        if (roleId == Guid.Empty) throw new ArgumentException("RoleId is required.", nameof(roleId));

        TenantId = tenantId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        RoleId = roleId;
        PasswordHash = null!;
        Role = null!;
        IsActive = true;
        IsMfaEnabled = false;
        IsEmailVerified = false;
    }

    public static User Register(Guid tenantId, string firstName, string lastName, string email, Guid roleId, LegalConsent consent, bool marketingConsent)
    {
        var user = new User(tenantId, firstName, lastName, new EmailAddress(email), roleId)
        {
            LegalConsent = consent,
            MarketingConsent = marketingConsent
        };

        user.AddDomainEvent(new UserRegisteredEvent(user.Id, user.TenantId, user.Email!, user.FirstName));

        return user;
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

    public void VerifyEmail()
    {
        IsEmailVerified = true;
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

    public LoginSession AddSession(string ipAddress, string? deviceFingerprint, string? userAgent = null)
    {
        // 1. Deduplication: Reuse existing session if fingerprint matches and not revoked
        if (!string.IsNullOrEmpty(deviceFingerprint))
        {
            var existingSession = _sessions.FirstOrDefault(s => s.DeviceFingerprint == deviceFingerprint && !s.IsRevoked);
            if (existingSession != null)
            {
                existingSession.UpdateActivity();
                return existingSession;
            }
        }

        // 2. Limit Enforcement: Max 10 active sessions. Revoke oldest if limit exceeded.
        var activeSessions = _sessions.Where(s => !s.IsRevoked).OrderBy(s => s.LastActiveAt).ToList();
        if (activeSessions.Count >= 10)
        {
            // Revoke the oldest one
            var oldest = activeSessions.First();
            oldest.Revoke();
        }

        var session = new LoginSession(TenantId, Id, ipAddress, deviceFingerprint, userAgent);
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

    public UserSecurityToken AddSecurityToken(string token, SecurityTokenType type, int expiryMinutes)
    {
        var securityToken = new UserSecurityToken(Id, token, type, expiryMinutes);
        _securityTokens.Add(securityToken);
        return securityToken;
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

    public bool DetectAndAlertNewDevice(string ipAddress, string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return false;

        // Use IP segment to be resilient to minor ISP changes
        var ipParts = ipAddress.Split('.');
        var ipSegment = ipParts.Length >= 2 ? $"{ipParts[0]}.{ipParts[1]}" : ipAddress;

        var isKnown = _sessions.Any(s =>
            s.UserAgent == userAgent &&
            s.IpAddress.StartsWith(ipSegment) &&
            !s.IsRevoked);

        if (!isKnown)
        {
            AddDomainEvent(new NewDeviceLoginEvent(this, ipAddress, userAgent));
            return true;
        }

        return false;
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
