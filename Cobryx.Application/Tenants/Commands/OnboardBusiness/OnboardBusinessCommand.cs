using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Common;
using Concordia;
using FluentValidation;

namespace Cobryx.Application.Tenants.Commands.OnboardBusiness;

public record OnboardBusinessCommand(
    string TaxId,
    string Industry,
    string BusinessAddress,
    string? Phone = null) : IRequest<Result>;

public class OnboardBusinessValidator : AbstractValidator<OnboardBusinessCommand>
{
    public OnboardBusinessValidator()
    {
        RuleFor(x => x.TaxId)
            .NotEmpty()
            .MaximumLength(13)
            .Matches(@"^[A-Z&Ñ]{3,4}[0-9]{2}(0[1-9]|1[0-2])(0[1-9]|[12][0-9]|3[01])[A-Z0-9]{2}[0-9A]$")
            .WithMessage("Invalid Tax ID (RFC) format.");

        RuleFor(x => x.Industry).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BusinessAddress).NotEmpty().MaximumLength(500);
    }
}

public class OnboardBusinessHandler : IRequestHandler<OnboardBusinessCommand, Result>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly ICurrentUserProvider _userProvider;
    private readonly IUnitOfWork _unitOfWork;

    public OnboardBusinessHandler(
        ITenantRepository tenantRepository,
        IUserRepository userRepository,
        ITenantProvider tenantProvider,
        ICurrentUserProvider userProvider,
        IUnitOfWork unitOfWork)
    {
        _tenantRepository = tenantRepository;
        _userRepository = userRepository;
        _tenantProvider = tenantProvider;
        _userProvider = userProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(OnboardBusinessCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        var userId = _userProvider.GetUserId();

        if (tenantId == null || userId == null) return Result.Failure("Unauthorized context.");

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null) return Result.Failure("User not found.");

        if (!user.RequiresOnboarding) return Result.Failure("Onboarding already completed.");

        var tenant = await _tenantRepository.GetByIdAsync(tenantId.Value, cancellationToken);
        if (tenant == null) return Result.Failure("Tenant not found.");

        if (tenant.OnboardingStatus == Cobryx.Domain.Enums.TenantOnboardingStatus.Completed)
        {
            user.CompleteOnboarding();
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure("Business onboarding already completed.");
        }

        tenant.UpdateOnboardingInfo(request.TaxId, request.Industry, request.BusinessAddress, request.Phone);
        user.CompleteOnboarding();

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
