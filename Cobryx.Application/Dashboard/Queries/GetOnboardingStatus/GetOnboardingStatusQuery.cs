using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Dashboard.Common;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Dashboard.Queries.GetOnboardingStatus;

public record GetOnboardingStatusQuery : IRequest<Result<OnboardingStatusDto>>;

public class GetOnboardingStatusHandler : IRequestHandler<GetOnboardingStatusQuery, Result<OnboardingStatusDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;

    public GetOnboardingStatusHandler(IUnitOfWork unitOfWork, ITenantProvider tenantProvider)
    {
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<OnboardingStatusDto>> Handle(GetOnboardingStatusQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? throw new DomainException(DomainErrorCode.Tenant.ContextMissing);
        var dbContext = (DbContext)_unitOfWork;

        var tenant = await dbContext.Set<Tenant>()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);

        var businessCompleted = tenant != null && tenant.TaxId != null && !string.IsNullOrEmpty(tenant.TaxId.Value);
        var usersCount = await dbContext.Set<User>()
            .CountAsync(u => u.TenantId == tenantId, cancellationToken);
        var firstUserInvited = usersCount > 1;

        var loans = await dbContext.Set<Loan>()
            .Where(l => l.TenantId == tenantId && !l.IsDeleted)
            .ToListAsync(cancellationToken);

        var firstLoanCreated = loans.Any(l => !l.IsDemo);
        var hasDemoLoans = loans.Any(l => l.IsDemo);

        var payments = await dbContext.Set<Cobryx.Domain.Payments.Payment>()
            .Where(p => p.TenantId == tenantId && !p.IsDeleted && p.Status == PaymentStatus.Completed)
            .ToListAsync(cancellationToken);

        var firstPaymentRegistered = payments.Any(p => !p.IsDemo);
        var hasDemoPayments = payments.Any(p => p.IsDemo);

        var hasSeenValueHabit = firstLoanCreated;

        var score = 0;
        var milestone = OnboardingMilestone.EstablishingFoundation;

        if (businessCompleted) score += 20;
        if (firstUserInvited) score += 20;
        if (firstLoanCreated || hasDemoLoans) score += 30;
        if (firstPaymentRegistered || hasDemoPayments) score += 30;

        var isDemoProgress = (hasDemoLoans || hasDemoPayments) && !firstLoanCreated && !firstPaymentRegistered;

        milestone = score switch
        {
            < 20 => OnboardingMilestone.EstablishingFoundation,
            < 40 => OnboardingMilestone.BuildingTeam,
            < 70 => OnboardingMilestone.CreatingAssets,
            < 100 => OnboardingMilestone.RealizingValue,
            _ => OnboardingMilestone.GrowthReady
        };

        return Result.Success(new OnboardingStatusDto(
            businessCompleted,
            firstUserInvited,
            firstLoanCreated,
            firstPaymentRegistered,
            hasSeenValueHabit,
            score,
            milestone,
            isDemoProgress
        ));
    }
}
