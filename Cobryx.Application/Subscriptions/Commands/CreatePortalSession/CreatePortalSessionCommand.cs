using Cobryx.Domain.Common;
using Concordia;

namespace Cobryx.Application.Subscriptions.Commands.CreatePortalSession;

public record CreatePortalSessionCommand(string? ReturnUrl = null) : IRequest<Result<string>>;
