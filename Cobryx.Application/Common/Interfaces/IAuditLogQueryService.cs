using Cobryx.Application.Common.Models;

namespace Cobryx.Application.Common.Interfaces;

public interface IAuditLogQueryService
{
    Task<PaginatedList<AuditLogEntry>> QueryAsync(
        Guid tenantId,
        int page,
        int pageSize,
        string? entityName = null,
        string? action = null,
        Guid? userId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// A detailed record of an administrative or system action for the audit trail.
/// </summary>
public record AuditLogEntry(
    Guid Id,
    string EntityName,
    string EntityId,
    string Action,
    Guid? UserId,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    string? UserAgent,
    DateTime CreatedAt
)
{
    /// <summary>Unique identifier for the audit record.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; init; } = Id;

    /// <summary>Technical name of the affected entity (e.g., "Loan", "Customer").</summary>
    /// <example>Loan</example>
    public string EntityName { get; init; } = EntityName;

    /// <summary>Unique identifier of the domain record that was modified.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa7</example>
    public string EntityId { get; init; } = EntityId;

    /// <summary>The type of operation performed (Created, Updated, Deleted).</summary>
    /// <example>Updated</example>
    public string Action { get; init; } = Action;

    /// <summary>Identifier of the user who performed the action.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa8</example>
    public Guid? UserId { get; init; } = UserId;

    /// <summary>JSON snapshot of the entity state before the change.</summary>
    /// <example>{"Status": "Pending", "Amount": 1000}</example>
    public string? OldValues { get; init; } = OldValues;

    /// <summary>JSON snapshot of the entity state after the change.</summary>
    /// <example>{"Status": "Active", "Amount": 1000}</example>
    public string? NewValues { get; init; } = NewValues;

    /// <summary>The IP address from which the action was initiated.</summary>
    /// <example>192.168.1.1</example>
    public string? IpAddress { get; init; } = IpAddress;

    /// <summary>The user agent string of the client application.</summary>
    /// <example>Mozilla/5.0 (Windows NT 10.0; Win64; x64)...</example>
    public string? UserAgent { get; init; } = UserAgent;

    /// <summary>Timestamp of when the audit entry was created (UTC).</summary>
    /// <example>2026-02-05T17:33:08Z</example>
    public DateTime CreatedAt { get; init; } = CreatedAt;
}
