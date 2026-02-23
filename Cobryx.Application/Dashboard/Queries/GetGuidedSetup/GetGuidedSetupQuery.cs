using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Dashboard.Common;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Exceptions;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Dashboard.Queries.GetGuidedSetup;

public record GetGuidedSetupQuery : IRequest<Result<NextBestActionDto>>;

public class GetGuidedSetupHandler : IRequestHandler<GetGuidedSetupQuery, Result<NextBestActionDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public GetGuidedSetupHandler(IUnitOfWork unitOfWork, ITenantProvider tenantProvider)
    {
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<NextBestActionDto>> Handle(GetGuidedSetupQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? throw new DomainException(DomainErrorCode.Tenant.ContextMissing);
        var dbContext = (DbContext)_unitOfWork;

        var tenant = await dbContext.Set<Domain.Entities.Tenant>()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        var usersCount = await dbContext.Set<Domain.Entities.User>()
            .CountAsync(u => u.TenantId == tenantId, cancellationToken);

        var activeLoansCount = await dbContext.Set<Domain.Entities.Lending.Loan>()
            .CountAsync(l => l.TenantId == tenantId && !l.IsDeleted, cancellationToken);

        var paymentsCount = await dbContext.Set<Domain.Entities.Payments.Payment>()
            .CountAsync(p => p.TenantId == tenantId && !p.IsDeleted && p.Status == Domain.Enums.PaymentStatus.Completed, cancellationToken);

        var context = new OnboardingContextDto(
            activeLoansCount,
            usersCount,
            paymentsCount,
            activeLoansCount > 0
        );

        if (tenant == null || tenant.TaxId == null || string.IsNullOrEmpty(tenant.TaxId.Value))
        {
            return Result.Success(new NextBestActionDto(
                OnboardingAction.CompleteBusinessProfile,
                OnboardingReason.MissingBusinessData,
                1,
                context));
        }

        if (usersCount <= 1)
        {
            return Result.Success(new NextBestActionDto(
                OnboardingAction.InviteFirstUser,
                OnboardingReason.NoUsers,
                2,
                context));
        }

        if (activeLoansCount == 0)
        {
            return Result.Success(new NextBestActionDto(
                OnboardingAction.CreateFirstLoan,
                OnboardingReason.NoLoans,
                3,
                context));
        }

        if (paymentsCount == 0)
        {
            return Result.Success(new NextBestActionDto(
                OnboardingAction.RegisterFirstPayment,
                OnboardingReason.NoPayments,
                4,
                context));
        }

        return Result.Success(new NextBestActionDto(
            OnboardingAction.ViewDashboard,
            OnboardingReason.NoDashboardData,
            5,
            context));
    }
}
