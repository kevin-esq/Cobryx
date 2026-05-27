using Cobryx.Domain.Shared;
using Cobryx.Application.Common.Interfaces;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Subscriptions.Commands.CreatePortalSession;

[TenantScoped]
public record CreatePortalSessionCommand(string? ReturnUrl = null) : IRequest<Result<string>>, IRequiresTenant;
