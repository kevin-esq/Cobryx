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
);
