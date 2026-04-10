using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Collections.Queries.GetPriorityCases;

/// <summary>
/// Query to retrieve priority collection cases from Redis sorted set.
/// Requires tenant context - validated by TenantValidationBehavior.
/// </summary>
[TenantScoped]
public record GetPriorityCasesQuery(int Limit = 100) : IRequest<Result<List<PriorityCaseDto>>>, IRequiresTenant;

public record PriorityCaseDto(double PriorityScore, object? Data);

public class GetPriorityCasesHandler(
    ICollectionsPriorityStore priorityStore,
    ITenantProvider tenantProvider) : IRequestHandler<GetPriorityCasesQuery, Result<List<PriorityCaseDto>>>
{
    public async Task<Result<List<PriorityCaseDto>>> Handle(GetPriorityCasesQuery request, CancellationToken cancellationToken)
    {
        // Tenant already validated by TenantValidationBehavior
        Guid tenantId = tenantProvider.GetTenantId()!.Value;

        List<PriorityCaseEntry> entries = await priorityStore.GetTopPriorityCasesAsync(
            tenantId, request.Limit, cancellationToken);

        var results = entries
            .Select(e => new PriorityCaseDto(e.PriorityScore, e.Metadata))
            .ToList();

        return Result.Success(results);
    }
}
