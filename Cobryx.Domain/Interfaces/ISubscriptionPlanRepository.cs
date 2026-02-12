using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Interfaces;

public interface ISubscriptionPlanRepository : IRepository<SubscriptionPlan>
{
    Task<List<SubscriptionPlan>> GetActivePlansAsync(CancellationToken ct = default);
}
