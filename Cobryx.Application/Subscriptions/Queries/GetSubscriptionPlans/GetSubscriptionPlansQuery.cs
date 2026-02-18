using Cobryx.Application.Subscriptions.Common;
using Cobryx.Domain.Common;
using Concordia;
using System.Collections.Generic;

namespace Cobryx.Application.Subscriptions.Queries.GetSubscriptionPlans;

public record GetSubscriptionPlansQuery : IRequest<Result<List<PlanDto>>>;
