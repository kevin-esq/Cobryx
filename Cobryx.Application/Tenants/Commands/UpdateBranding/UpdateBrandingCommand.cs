using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Tenants.Commands.UpdateBranding;

[TenantScoped]
public record UpdateBrandingCommand(
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor
) : IRequest<Result>, IRequiresTenant;
