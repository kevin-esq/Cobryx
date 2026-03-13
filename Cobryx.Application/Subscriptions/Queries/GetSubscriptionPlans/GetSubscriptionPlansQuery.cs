using Cobryx.Application.Subscriptions.Common;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Subscriptions.Queries.GetSubscriptionPlans;

public record GetSubscriptionPlansQuery : IRequest<Result<List<PlanDto>>>;
