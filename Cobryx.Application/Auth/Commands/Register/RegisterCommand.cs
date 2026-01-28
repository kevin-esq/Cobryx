using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Auth.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;

namespace Cobryx.Application.Auth.Commands.Register;

public record RegisterCommand(
    string BusinessName,
    string FirstName,
    string LastName,
    string Email,
    string Password) : IRequest<Result<AuthResult>>;

public class RegisterHandler : IRequestHandler<RegisterCommand, Result<AuthResult>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthService _authService;
    private readonly IHttpContextService _httpContextService;

    public RegisterHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IAuthService authService,
        IHttpContextService httpContextService)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _authService = authService;
        _httpContextService = httpContextService;
    }

    public async Task<Result<AuthResult>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email))
        {
            return Result.Failure<AuthResult>("Email already registered.");
        }

        var tenant = new Tenant(request.BusinessName);
        await _tenantRepository.AddAsync(tenant);

        var ownerRole = await _roleRepository.GetByNameAsync("Owner");
        if (ownerRole == null)
        {
            return Result.Failure<AuthResult>("System roles not initialized.");
        }

        var user = new User(tenant.Id, request.FirstName, request.LastName, request.Email, ownerRole.Id);
        user.SetPasswordHash(_passwordHasher.HashPassword(request.Password));
        user.CreateProfile();

        var ipAddress = _httpContextService.GetIpAddress();
        var deviceFingerprint = _httpContextService.GetDeviceFingerprint();
        var authResult = _authService.GenerateAuthResponse(user, ipAddress, deviceFingerprint, ownerRole);

        await _userRepository.AddAsync(user);

        return Result.Success(authResult);
    }
}
