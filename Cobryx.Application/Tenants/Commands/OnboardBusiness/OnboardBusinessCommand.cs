using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Validation;
using Cobryx.Domain.Exceptions.Common;
using Cobryx.Domain.Exceptions.Tenants;
using Cobryx.Domain.Exceptions.Users;
using Cobryx.Domain.Identity.Enums;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using FluentValidation;

namespace Cobryx.Application.Tenants.Commands.OnboardBusiness;

[TenantScoped]
public record OnboardBusinessCommand(
    string TaxId,
    string Industry,
    string BusinessAddress,
    string? Phone = null) : IRequest<Result>, IRequiresTenant;

public class OnboardBusinessValidator : AbstractValidator<OnboardBusinessCommand>
{
    public OnboardBusinessValidator()
    {
        RuleFor(x => x.TaxId)
            .NotEmpty().WithErrorCode(TenantValidationErrors.TaxId.Required)
            .MaximumLength(13).WithErrorCode(TenantValidationErrors.TaxId.TooLong)
            .Matches(@"^[A-Z&Ñ]{3,4}[0-9]{2}(0[1-9]|1[0-2])(0[1-9]|[12][0-9]|3[01])[A-Z0-9]{2}[0-9A]$")
            .WithErrorCode(TenantValidationErrors.TaxId.Invalid);

        RuleFor(x => x.Industry)
            .NotEmpty().WithErrorCode(TenantValidationErrors.Industry.Required)
            .MaximumLength(100).WithErrorCode(TenantValidationErrors.Industry.TooLong);
        RuleFor(x => x.BusinessAddress)
            .NotEmpty().WithErrorCode(TenantValidationErrors.BusinessAddress.Required)
            .MaximumLength(500).WithErrorCode(TenantValidationErrors.BusinessAddress.TooLong);
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

        if (tenantId == null || userId == null)
            throw new UnauthorizedContextException();

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null)
            throw new UserNotFoundException(userId.Value);

        if (!user.RequiresOnboarding)
            throw new OnboardingCompletedException();

        var tenant = await _tenantRepository.GetByIdAsync(tenantId.Value, cancellationToken);
        if (tenant == null)
            throw new TenantNotFoundException(tenantId.Value);

        if (tenant.OnboardingStatus == TenantOnboardingStatus.Completed)
        {
            user.CompleteOnboarding();
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new OnboardingCompletedException();
        }

        tenant.UpdateOnboardingInfo(request.TaxId, request.Industry, request.BusinessAddress, request.Phone);
        user.CompleteOnboarding();

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _userRepository.UpdateAsync(user, cancellationToken);

        tenant.TriggerOnboardingMilestone("ESTABLISHING_FOUNDATION");

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
