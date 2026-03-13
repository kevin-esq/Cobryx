using Cobryx.Application.Subscriptions.Common;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Subscriptions.Queries.GetSubscriptionStatus;

public record GetSubscriptionStatusQuery : IRequest<Result<SubscriptionStatusDto>>;
