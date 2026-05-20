using Cobryx.Domain.Events;
using Cobryx.Domain.Identity.Enums;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Identity;

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
    public bool IsLocked { get; private set; }
    public bool RequiresOnboarding { get; private set; }
    public LegalConsent? LegalConsent { get; private set; }
    public bool MarketingConsent { get; private set; }
    public DateTime? LastVerificationSentAt { get; private set; }
    public int VerificationResendCount { get; private set; }
    public int PermissionVersion { get; private set; }

    public virtual UserProfile? Profile { get; private set; }
    private readonly List<LoginSession> _sessions = new();
    public IReadOnlyCollection<LoginSession> Sessions => _sessions.AsReadOnly();

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
        if (tenantId == Guid.Empty)
            throw new DomainException(DomainErrorCode.User.TenantIdRequired);
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException(DomainErrorCode.User.FirstNameRequired);
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException(DomainErrorCode.User.LastNameRequired);
        if (email == null)
            throw new DomainException(DomainErrorCode.User.EmailRequired);
        if (roleId == Guid.Empty)
            throw new DomainException(DomainErrorCode.User.RoleIdRequired);

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
        IsLocked = false;
        RequiresOnboarding = true;
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

    public void EnableMfa() => EnableMfa(DateTime.UtcNow);

    public void EnableMfa(DateTime now)
    {
        IsMfaEnabled = true;
        MfaEnabledAt = now;
        UpdateTimestamp(now);
    }

    public void DisableMfa()
    {
        IsMfaEnabled = false;
        MfaEnabledAt = null;
        _mfaDevices.Clear();
        _recoveryCodes.Clear();
        UpdateTimestamp();
    }

    public void CompleteOnboarding()
    {
        RequiresOnboarding = false;
        UpdateTimestamp();
    }

    public void Lock()
    {
        IsLocked = true;
        UpdateTimestamp();
    }

    public void Unlock()
    {
        IsLocked = false;
        UpdateTimestamp();
    }

    public void VerifyEmail()
    {
        IsEmailVerified = true;
        VerificationResendCount = 0;
        UpdateTimestamp();
    }

    public void UpdateVerificationResend()
    {
        LastVerificationSentAt = DateTime.UtcNow;
        VerificationResendCount++;
        UpdateTimestamp();
    }

    public void ResetVerificationResendCount()
    {
        VerificationResendCount = 0;
        UpdateTimestamp();
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException(DomainErrorCode.User.PasswordHashRequired);

        PasswordHash = passwordHash;
    }

    public void UpdateRole(Guid roleId)
    {
        if (roleId == Guid.Empty)
            throw new DomainException(DomainErrorCode.User.RoleIdRequired);
        RoleId = roleId;
    }

    public LoginSession AddSession(string ipAddress, string? deviceFingerprint, string? userAgent = null, string? deviceName = null)
    {
        if (!string.IsNullOrEmpty(deviceFingerprint))
        {
            // Only reuse sessions that were persisted (CreatedAt is set on first SaveChanges). Otherwise a
            // not-yet-saved session can match the same fingerprint and UpdateActivity() leaves EF tracking it
            // as Modified with no DB row, breaking the next SaveChanges (SQLite / concurrency retries).
            var existingSession = _sessions.FirstOrDefault(s =>
                s.DeviceFingerprint == deviceFingerprint &&
                !s.IsRevoked &&
                s.CreatedAt.Year > 1900);
            if (existingSession != null)
            {
                existingSession.UpdateActivity();
                return existingSession;
            }
        }

        var activeSessions = _sessions.Where(s => !s.IsRevoked).OrderBy(s => s.LastActiveAt).ToList();
        if (activeSessions.Count >= 10)
        {
            var oldest = activeSessions.FirstOrDefault();
            if (oldest != null)
                oldest.Revoke();
        }

        var session = new LoginSession(TenantId, Id, ipAddress, deviceFingerprint, userAgent, deviceName);
        _sessions.Add(session);
        return session;
    }

    public void RevokeSession(Guid sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session != null)
        {
            session.Revoke();
        }
    }

    public void AddMfaDevice(MfaDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);
        _mfaDevices.Add(device);
        UpdateTimestamp();
    }

    public void AddRecoveryCode(RecoveryCode code)
    {
        ArgumentNullException.ThrowIfNull(code);
        _recoveryCodes.Add(code);
        UpdateTimestamp();
    }

    public UserSecurityToken AddSecurityToken(string tokenHash, SecurityTokenType type, int expiryMinutes)
    {
        var securityToken = new UserSecurityToken(Id, tokenHash, type, expiryMinutes);
        _securityTokens.Add(securityToken);
        return securityToken;
    }

    public void CreateProfile(string? phoneNumber = null, string? avatarUrl = null)
    {
        if (Profile != null)
            return;
        Profile = new UserProfile(Id, phoneNumber, avatarUrl);
    }

    public bool DetectAndAlertNewDevice(string ipAddress, string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return false;

        var ipParts = ipAddress.Split('.');
        var ipSegment = ipParts.Length >= 2 ? $"{ipParts[0]}.{ipParts[1]}" : ipAddress;

        var isKnown = _sessions.Any(s =>
            s.UserAgent == userAgent &&
            s.IpAddress.StartsWith(ipSegment) &&
            !s.IsRevoked);

        if (!isKnown)
        {
            AddDomainEvent(new NewDeviceLoginEvent(Id, Email.Value, ipAddress, userAgent));
            return true;
        }

        return false;
    }

    public void IncrementPermissionVersion()
    {
        PermissionVersion++;
        UpdateTimestamp();
    }
}
