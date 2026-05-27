using Cobryx.Application.Subscriptions.Common;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Subscriptions.Queries.GetSubscriptionStatus;

[TenantScoped]
public record GetSubscriptionStatusQuery : IRequest<Result<SubscriptionStatusDto>>, IRequiresTenant;
