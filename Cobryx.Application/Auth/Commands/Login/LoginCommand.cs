using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Auth.Common;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<AuthResult>>;

public class LoginHandler : IRequestHandler<LoginCommand, Result<AuthResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthService _authService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<LoginHandler> _logger;
    private readonly IHttpContextService _httpContextService;

    public LoginHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IAuthService authService,
        ITenantProvider tenantProvider,
        ILogger<LoginHandler> logger,
        IHttpContextService httpContextService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _authService = authService;
        _tenantProvider = tenantProvider;
        _logger = logger;
        _httpContextService = httpContextService;
    }

    public async Task<Result<AuthResult>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login attempt for email: {Email}", request.Email);

        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Authentication failed for email: {Email}", request.Email);
            return Result.Failure<AuthResult>("Invalid credentials.");
        }

        var ipAddress = _httpContextService.GetIpAddress();
        var deviceFingerprint = _httpContextService.GetDeviceFingerprint();
        var authResult = _authService.GenerateAuthResponse(user, ipAddress, deviceFingerprint);

        _tenantProvider.SetTenantId(user.TenantId);

        await _userRepository.UpdateAsync(user);

        return Result.Success(authResult);
    }
}