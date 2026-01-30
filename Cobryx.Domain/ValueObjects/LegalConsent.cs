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
            throw new ArgumentException("Consent must be accepted.", nameof(accepted));
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Document version is required.", nameof(version));
        if (string.IsNullOrWhiteSpace(ipAddress))
            throw new ArgumentException("IP address is required for legal traceability.", nameof(ipAddress));
        if (string.IsNullOrWhiteSpace(userAgent))
            throw new ArgumentException("User-Agent is required for legal traceability.", nameof(userAgent));

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
