using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Models;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Audit.Queries.GetAuditLogs
{
    public record GetAuditLogsQuery(
        int Page = 1,
        int PageSize = 20,
        string? EntityName = null,
        string? Action = null,
        Guid? UserId = null,
        DateTime? From = null,
        DateTime? To = null) : IRequest<Result<PaginatedList<AuditLogEntry>>>;

    public class GetAuditLogsHandler(IAuditLogQueryService auditLogQuery, ITenantProvider tenantProvider) : IRequestHandler<GetAuditLogsQuery, Result<PaginatedList<AuditLogEntry>>>
    {
        private readonly IAuditLogQueryService _auditLogQuery = auditLogQuery;
        private readonly ITenantProvider _tenantProvider = tenantProvider;

        public async Task<Result<PaginatedList<AuditLogEntry>>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            var tenantId = _tenantProvider.GetTenantId();
            if (!tenantId.HasValue)
            {
                return Result.Failure<PaginatedList<AuditLogEntry>>(DomainErrorCode.Tenant.ContextMissing);
            }

            var result = await _auditLogQuery.QueryAsync(
                tenantId.Value,
                request.Page,
                request.PageSize,
                request.EntityName,
                request.Action,
                request.UserId,
                request.From,
                request.To,
                cancellationToken);

            return Result.Success(result);
        }
    }
}
