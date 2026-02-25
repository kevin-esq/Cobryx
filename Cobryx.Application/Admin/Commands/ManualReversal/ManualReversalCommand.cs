using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Lending.Services;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Admin.Commands.ManualReversal;

public record ManualReversalCommand(Guid TransactionId, decimal Amount, string Reason) : IRequest<Result>;

public class ManualReversalHandler : IRequestHandler<ManualReversalCommand, Result>
{
    private readonly ICobryxDbContext _dbContext;
    private readonly FinancialPostingEngine _postingEngine;
    private readonly FinancialStateEngine _stateEngine;
    private readonly ICurrentUserProvider _currentUserProvider;

    public ManualReversalHandler(
        ICobryxDbContext dbContext,
        FinancialPostingEngine postingEngine,
        FinancialStateEngine stateEngine,
        ICurrentUserProvider currentUserProvider)
    {
        _dbContext = dbContext;
        _postingEngine = postingEngine;
        _stateEngine = stateEngine;
        _currentUserProvider = currentUserProvider;
    }

    public async Task<Result> Handle(ManualReversalCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            return Result.Failure(DomainErrorCode.Common.ReasonRequired);

        // 1. Find original transaction
        var originalTx = await _dbContext.LedgerTransactions
            .Include(t => t.Entries)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, ct);

        if (originalTx == null)
            return Result.Failure(DomainErrorCode.Accounting.TransactionNotFound);

        if (originalTx.IsReversal)
            return Result.Failure(DomainErrorCode.Accounting.CannotReverseReversal);

        // 2. BANK-GRADE: Execution through core engine
        // This ensures the reversal is mirrored and platform fees are handled proportionally.
        var reversalId = await _postingEngine.PostReversalAsync(
            originalTx.Id,
            request.Amount,
            request.Reason,
            ct);

        // 3. Update Financial State (if linked to a loan)
        if (originalTx.LoanId.HasValue)
        {
            await _stateEngine.UpdateStatusAsync(originalTx.LoanId.Value, $"Manual Reversal: {request.Reason}", ct);
        }

        // 4. Audit Trail
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
