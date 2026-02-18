using Cobryx.Application.Subscriptions.Common;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cobryx.Application.Subscriptions.Queries.GetSubscriptionPlans;

public class GetSubscriptionPlansHandler : IRequestHandler<GetSubscriptionPlansQuery, Result<List<PlanDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSubscriptionPlansHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<PlanDto>>> Handle(GetSubscriptionPlansQuery request, CancellationToken cancellationToken)
    {
        var dbContext = (DbContext)_unitOfWork;
        var plans = await dbContext.Set<SubscriptionPlan>()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Tier)
            .Select(p => new PlanDto(
                p.Id,
                p.Name,
                p.Description,
                p.Price.Amount,
                p.Price.Currency,
                p.MaxInvoices,
                p.MaxUsers,
                p.Tier.ToString(),
                p.TrialDays))
            .ToListAsync(cancellationToken);

        return Result.Success(plans);
    }
}
