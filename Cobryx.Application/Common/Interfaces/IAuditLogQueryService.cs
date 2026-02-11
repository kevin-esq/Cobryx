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
    DateTime CreatedAt);
