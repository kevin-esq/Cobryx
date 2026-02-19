using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Dashboard.Common;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
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

        // 1. Business Profile Completion
        var tenant = await dbContext.Set<Domain.Entities.Tenant>()
            .FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        
        var businessCompleted = tenant != null && !string.IsNullOrEmpty(tenant.TaxId);

        // 2. First User Invited (other than owner)
        var usersCount = await dbContext.Set<Domain.Entities.User>()
            .CountAsync(u => u.TenantId == tenantId, cancellationToken);
        var firstUserInvited = usersCount > 1;

        // 3. First Loan Created
        var firstLoanCreated = await dbContext.Set<Domain.Entities.Lending.Loan>()
            .AnyAsync(l => l.TenantId == tenantId && !l.IsDeleted, cancellationToken);

        // 4. First Payment Registered
        var firstPaymentRegistered = await dbContext.Set<Domain.Entities.Payments.Payment>()
            .AnyAsync(p => p.TenantId == tenantId && !p.IsDeleted && p.Status == Domain.Enums.PaymentStatus.Completed, cancellationToken);

        // 5. Habit: Seen Dashboard with Data
        var hasSeenValueHabit = firstLoanCreated;

        // 6. Progress & Milestone Algorithm
        var score = 0;
        var milestone = OnboardingMilestone.EstablishingFoundation;

        if (businessCompleted) score += 20;
        if (firstUserInvited) score += 20;
        if (firstLoanCreated) score += 30;
        if (firstPaymentRegistered) score += 30;

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
            milestone
        ));
    }
}
