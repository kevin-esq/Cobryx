using Cobryx.Application.Analytics.Models;
using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Analytics.Queries.GetPortfolioAging
{
    /// <summary>
    /// Query to retrieve portfolio aging buckets from cache.
    /// Requires tenant context - validated by TenantValidationBehavior.
    /// </summary>
    [TenantScoped]
    public record GetPortfolioAgingQuery : IRequest<Result<PortfolioAgingCache>>, IRequiresTenant;

    public class GetPortfolioAgingHandler(
        ITenantProvider tenantProvider,
        ICacheService cache) : IRequestHandler<GetPortfolioAgingQuery, Result<PortfolioAgingCache>>
    {
        public async Task<Result<PortfolioAgingCache>> Handle(GetPortfolioAgingQuery request,
            CancellationToken cancellationToken)
        {
            // Tenant already validated by TenantValidationBehavior
            Guid tenantId = tenantProvider.GetTenantId()!.Value;

            var key = $"portfolio:aging:{tenantId}";
            PortfolioAgingCache? aging = await cache.GetAsync<PortfolioAgingCache>(key, cancellationToken);

            return aging == null
                ? Result.Failure<PortfolioAgingCache>(DomainErrorCode.Common.EntityNotFound)
                : Result.Success(aging);
        }
    }
}
