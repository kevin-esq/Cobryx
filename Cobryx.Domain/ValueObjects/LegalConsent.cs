using System;
using System.Collections.Generic;
using Cobryx.Domain.Common;

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
            throw new DomainException("DOMAIN.CONSENT_REQUIRED");
        if (string.IsNullOrWhiteSpace(version))
            throw new DomainException("DOMAIN.INVALID_CONSENT_VERSION");
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new DomainException("DOMAIN.IP_ADDRESS_REQUIRED");
        if (string.IsNullOrWhiteSpace(userAgent))
            throw new DomainException("DOMAIN.USER_AGENT_REQUIRED");

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
