using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Lending.Services;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Shared;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Operations.Commands.ManualChargeOff;

[PlatformScoped]
public record ManualChargeOffCommand(Guid LoanId, string Reason) : IRequest<Result>;

public class ManualChargeOffHandler(
    ICobryxDbContext dbContext,
    FinancialStateEngine stateEngine,
    ICurrentUserProvider currentUserProvider) : IRequestHandler<ManualChargeOffCommand, Result>
{
    private readonly ICobryxDbContext _dbContext = dbContext;
    private readonly FinancialStateEngine _stateEngine = stateEngine;
    private readonly ICurrentUserProvider _currentUserProvider = currentUserProvider;

    public async Task<Result> Handle(ManualChargeOffCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure(DomainErrorCode.Common.ReasonRequired);

        await _stateEngine.ExecuteChargeOffAsync(request.LoanId, request.Reason, ct);

        var adminUserId = _currentUserProvider.GetUserId() ?? Guid.Empty;
        var audit = new AdminActionAudit(
            adminUserId: adminUserId,
            actionName: "Loan.ManualChargeOff",
            targetType: "Loan",
            targetId: request.LoanId,
            reason: request.Reason
        );

        _dbContext.AdminActionAudits.Add(audit);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
