using Cobryx.Domain.Common;
using Concordia;

namespace Cobryx.Application.Tenants.Commands.UpdateBranding;

public record UpdateBrandingCommand(
    string? LogoUrl,
    string? PrimaryColor,
    string? SecondaryColor
) : IRequest<Result>;
