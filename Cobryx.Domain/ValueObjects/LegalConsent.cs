using Cobryx.Domain.Shared;

namespace Cobryx.Domain.ValueObjects;

public record LegalConsent : ValueObject
{
    public bool Accepted { get; }
    public DateTime Timestamp { get; }
    public string Version { get; }
    public string IpAddress { get; }
    public string UserAgent { get; }

    private LegalConsent()
    {
        Version = null!;
        IpAddress = null!;
        UserAgent = null!;
    }

    public LegalConsent(bool accepted, string version, string ipAddress, string userAgent)
    {
        if (!accepted)
            throw new DomainException(DomainErrorCode.Legal.ConsentRequired);
        if (string.IsNullOrWhiteSpace(version))
            throw new DomainException(DomainErrorCode.Legal.InvalidConsentVersion);
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new DomainException(DomainErrorCode.Legal.IpAddressRequired);
        if (string.IsNullOrWhiteSpace(userAgent))
            throw new DomainException(DomainErrorCode.Legal.UserAgentRequired);

        Accepted = accepted;
        Timestamp = DateTime.UtcNow;
        Version = version;
        IpAddress = ipAddress;
        UserAgent = userAgent;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Accepted;
        yield return Timestamp;
        yield return Version;
        yield return IpAddress;
        yield return UserAgent;
    }
}
