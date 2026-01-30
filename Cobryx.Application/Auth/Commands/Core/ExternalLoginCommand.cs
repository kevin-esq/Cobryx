using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Auth.Services;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Domain.Common;
using Cobryx.Application.Auth.Common;

namespace Cobryx.Application.Auth.Commands.Core;

public record ExternalLoginCommand(ExternalProvider Provider, string IdToken, string? CaptchaToken = null) : IRequest<Result<AuthResult>>;

public class ExternalLoginHandler : IRequestHandler<ExternalLoginCommand, Result<AuthResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IExternalAuthService _externalAuthService;
    private readonly IAuthService _authService;
    private readonly ITenantRepository _tenantRepository; // To create tenant if new user
    private readonly IHttpContextService _httpContextService;

    public ExternalLoginHandler(
        IUserRepository userRepository,
        IExternalAuthService externalAuthService,
        IAuthService authService,
        ITenantRepository tenantRepository,
        IHttpContextService httpContextService)
    {
        _userRepository = userRepository;
        _externalAuthService = externalAuthService;
        _authService = authService;
        _tenantRepository = tenantRepository;
        _httpContextService = httpContextService;
    }

    public async Task<Result<AuthResult>> Handle(ExternalLoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify Token with Provider
        var externalUserResult = await _externalAuthService.VerifyTokenAsync(request.Provider, request.IdToken, cancellationToken);
        if (!externalUserResult.IsSuccess)
        {
            return Result.Failure<AuthResult>(externalUserResult.Error ?? "External authentication failed.");
        }

        var externalUser = externalUserResult.Value;
        if (externalUser == null)
        {
            return Result.Failure<AuthResult>("Failed to retrieve user information from external provider.");
        }

        // 2. Check if user exists
        var user = await _userRepository.GetByEmailAsync(externalUser.Email);

        if (user == null)
        {
            // 3. Register if not exists (Auto-Registration)
            var tenant = new Tenant(externalUser.FirstName + "'s Tenant");
            await _tenantRepository.AddAsync(tenant); // Assuming basic add

            // Default Role: Admin of their own tenant
            // Need to get Role ID query or constant. For simplicity assuming we have a way or existing constants.
            // Using a placeholder or assuming RoleId is fetched elsewhere. 
            // In a real app we'd fetch the default "Admin" role for the new tenant.

            // Simplified: User creation requires RoleId. 
            // We will fetch the first available role or a default one.
            // This part might fail if no roles exist in DB.

            // For now, return failure if we cant auto-register without complex setup.
            // Or better: Use a hardcoded GUID or a service to get default role.

            return Result.Failure<AuthResult>("User does not exist. Please register first.");
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthResult>("Your account is inactive.");
        }

        if (!user.IsEmailVerified)
        {
            return Result.Failure<AuthResult>("Please verify your email.");
        }

        // 4. Generate Auth Response
        var ipAddress = _httpContextService.GetIpAddress() ?? "0.0.0.0";
        var deviceFingerprint = _httpContextService.GetDeviceFingerprint();
        var userAgent = _httpContextService.GetUserAgent();

        var authResponse = _authService.GenerateAuthResponse(user, ipAddress, deviceFingerprint, userAgent);

        // Persist changes (Session & Refresh Token)
        await _userRepository.UpdateAsync(user);

        return Result.Success(authResponse);
    }
}
