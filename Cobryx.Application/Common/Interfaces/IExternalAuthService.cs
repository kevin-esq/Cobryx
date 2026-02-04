using Cobryx.Domain.Common;

namespace Cobryx.Application.Common.Interfaces;

public enum ExternalProvider { Google, Microsoft }

public record ExternalUserProfile(
    string ProviderId,
    string Email,
    string FirstName,
    string LastName,
    string? PictureUrl
);

public interface IExternalAuthService
{
    Task<Result<ExternalUserProfile>> VerifyTokenAsync(ExternalProvider provider, string token, CancellationToken cancellationToken = default);
}
