namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// Public-facing session details.
/// </summary>
public record SessionContract(
    Guid Id,
    string IpAddress,
    string? DeviceFingerprint,
    string? DeviceName,
    DateTime LastActiveAt,
    bool IsCurrent
)
{
    /// <summary>Unique identifier of the session.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; init; } = Id;

    /// <summary>IPV4 or IPV6 address of the device.</summary>
    /// <example>192.168.1.1</example>
    public string IpAddress { get; init; } = IpAddress;

    /// <summary>Unique fingerprint calculated on the client.</summary>
    /// <example>df_12345abcde</example>
    public string? DeviceFingerprint { get; init; } = DeviceFingerprint;

    /// <summary>User-friendly name of the device (if provided).</summary>
    /// <example>Chrome on MacOS</example>
    public string? DeviceName { get; init; } = DeviceName;

    /// <summary>Last recorded activity timestamp (UTC).</summary>
    /// <example>2026-02-16T14:34:01Z</example>
    public DateTime LastActiveAt { get; init; } = LastActiveAt;

    /// <summary>True if this session corresponds to the bearer of the token.</summary>
    /// <example>true</example>
    public bool IsCurrent { get; init; } = IsCurrent;
}
