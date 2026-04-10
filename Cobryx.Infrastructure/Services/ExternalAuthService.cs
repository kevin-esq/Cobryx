using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;
using Cobryx.Infrastructure.Configuration;

using Google.Apis.Auth;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cobryx.Infrastructure.Services;

public class ExternalAuthService : IExternalAuthService
{
    private readonly GoogleOAuthOptions _googleOptions;
    private readonly ILogger<ExternalAuthService> _logger;

    public ExternalAuthService(IOptions<GoogleOAuthOptions> googleOptions, ILogger<ExternalAuthService> logger)
    {
        _googleOptions = googleOptions.Value;
        _logger = logger;
    }

    public async Task<Result<ExternalUserProfile>> VerifyTokenAsync(ExternalProvider provider, string token, CancellationToken cancellationToken = default)
    {
        return provider switch
        {
            ExternalProvider.Google => await VerifyGoogleTokenAsync(token),
            ExternalProvider.Microsoft => Result.Failure<ExternalUserProfile>(DomainErrorCode.Auth.ExternalLoginFailed),
            _ => Result.Failure<ExternalUserProfile>(DomainErrorCode.Auth.ProviderNotSupported)
        };
    }

    private async Task<Result<ExternalUserProfile>> VerifyGoogleTokenAsync(string token)
    {
        try
        {
            var clientIds = _googleOptions.ClientId
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = clientIds
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(token, settings);

            return Result.Success(new ExternalUserProfile(
                payload.Subject,
                payload.Email,
                payload.GivenName,
                payload.FamilyName,
                payload.Picture
            ));
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Invalid Google Token");
            return Result.Failure<ExternalUserProfile>(DomainErrorCode.Auth.InvalidToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying Google Token");
            return Result.Failure<ExternalUserProfile>(DomainErrorCode.Auth.ExternalLoginFailed);
        }
    }
}
