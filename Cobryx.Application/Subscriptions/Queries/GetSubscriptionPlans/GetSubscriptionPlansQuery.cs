using Cobryx.Application.Subscriptions.Common;
using Cobryx.Domain.Shared;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Subscriptions.Queries.GetSubscriptionPlans;

[PublicRequest]
public record GetSubscriptionPlansQuery : IRequest<Result<List<PlanDto>>>;
