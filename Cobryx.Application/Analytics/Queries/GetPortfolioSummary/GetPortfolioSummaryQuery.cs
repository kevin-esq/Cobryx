using Cobryx.Application.Analytics.Models;
using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Analytics.Queries.GetPortfolioSummary
{
    /// <summary>
    /// Query to retrieve portfolio summary metrics from cache.
    /// Requires tenant context - validated by TenantValidationBehavior.
    /// </summary>
    [TenantScoped]
    public record GetPortfolioSummaryQuery : IRequest<Result<PortfolioSummaryCache>>, IRequiresTenant;

    public class GetPortfolioSummaryHandler(
        ITenantProvider tenantProvider,
        ICacheService cache) : IRequestHandler<GetPortfolioSummaryQuery, Result<PortfolioSummaryCache>>
    {
        public async Task<Result<PortfolioSummaryCache>> Handle(GetPortfolioSummaryQuery request,
            CancellationToken cancellationToken)
        {
            // Tenant already validated by TenantValidationBehavior
            Guid tenantId = tenantProvider.GetTenantId()!.Value;

            var key = $"portfolio:summary:{tenantId}";
            PortfolioSummaryCache? summary = await cache.GetAsync<PortfolioSummaryCache>(key, cancellationToken);

            return summary == null
                ? Result.Failure<PortfolioSummaryCache>(DomainErrorCode.Common.EntityNotFound)
                : Result.Success(summary);
        }
    }
}
