using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Models;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Services;

public class AuditLogQueryService : IAuditLogQueryService
{
    private readonly CobryxDbContext _dbContext;

    public AuditLogQueryService(CobryxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedList<AuditLogEntry>> QueryAsync(
        Guid tenantId,
        int page,
        int pageSize,
        string? entityName = null,
        string? action = null,
        Guid? userId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogEntry(
                a.Id,
                a.EntityName,
                a.EntityId,
                a.Action,
                a.UserId,
                a.OldValues,
                a.NewValues,
                a.IpAddress,
                a.UserAgent,
                a.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedList<AuditLogEntry>(
            items,
            totalCount,
            page,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }
}
