using Cobryx.Domain.Identity;

namespace Cobryx.Domain.Interfaces;

public interface ISubscriptionPlanRepository : IRepository<SubscriptionPlan>
{
    public Task<List<SubscriptionPlan>> GetActivePlansAsync(CancellationToken ct = default);
}
