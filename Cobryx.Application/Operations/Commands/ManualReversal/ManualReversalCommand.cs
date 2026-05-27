using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Lending.Services;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Operations.Commands.ManualReversal;

[PlatformScoped]
public record ManualReversalCommand(Guid TransactionId, decimal Amount, string Reason) : IRequest<Result>;

public class ManualReversalHandler(
    ICobryxDbContext dbContext,
    FinancialPostingEngine postingEngine,
    FinancialStateEngine stateEngine,
    ICurrentUserProvider currentUserProvider) : IRequestHandler<ManualReversalCommand, Result>
{
    private readonly ICobryxDbContext _dbContext = dbContext;
    private readonly FinancialPostingEngine _postingEngine = postingEngine;
    private readonly FinancialStateEngine _stateEngine = stateEngine;
    private readonly ICurrentUserProvider _currentUserProvider = currentUserProvider;

    public async Task<Result> Handle(ManualReversalCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure(DomainErrorCode.Common.ReasonRequired);

        var originalTx = await _dbContext.LedgerTransactions
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, ct);

        if (originalTx == null)
            return Result.Failure(DomainErrorCode.Accounting.TransactionNotFound);

        if (originalTx.IsReversal)
            return Result.Failure(DomainErrorCode.Accounting.CannotReverseReversal);

        var reversalId = await _postingEngine.PostReversalAsync(
            originalTx.Id,
            request.Amount,
            request.Reason,
            ct);

        if (originalTx.LoanId.HasValue)
        {
            await _stateEngine.UpdateStatusAsync(originalTx.LoanId.Value, $"Manual Reversal: {request.Reason}", ct);
        }

        var adminUserId = _currentUserProvider.GetUserId() ?? Guid.Empty;
        var audit = new AdminActionAudit(
            adminUserId: adminUserId,
            actionName: "Ledger.ManualReversal",
            targetType: "LedgerTransaction",
            targetId: originalTx.Id,
            reason: request.Reason,
            metadataJson: $"{{\"amount\":{request.Amount}, \"reversalId\":\"{reversalId}\"}}"
        );

        _dbContext.AdminActionAudits.Add(audit);
        await _dbContext.SaveChangesAsync(ct);

        return Result.Success();
    }
}
