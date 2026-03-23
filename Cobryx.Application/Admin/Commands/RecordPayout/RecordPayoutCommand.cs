using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Admin.Commands.RecordPayout;

public record RecordPayoutCommand(
    Guid TenantId,
    decimal Amount,
    string Currency,
    string Reference,
    string Status) : IRequest<Result>;

public class RecordPayoutHandler(ICobryxDbContext dbContext, ILogger<RecordPayoutHandler> logger) : IRequestHandler<RecordPayoutCommand, Result>
{
    private readonly ICobryxDbContext _dbContext = dbContext;
    private readonly ILogger<RecordPayoutHandler> _logger = logger;

    public async Task<Result> Handle(RecordPayoutCommand request, CancellationToken ct)
    {
        if (request.Status != "paid")
        {
            _logger.LogInformation("Payout {Reference} is in status {Status}. Skipping ledger entry.", request.Reference, request.Status);
            return Result.Success();
        }

        var stripeCashAcc = await GetOrCreateAccountAsync(request.TenantId, "1010", "Stripe Cash", LedgerAccountType.Asset, request.Currency, ct);
        var bankAcc = await GetOrCreateAccountAsync(request.TenantId, "1020", "Physical Bank", LedgerAccountType.Asset, request.Currency, ct);

        var transaction = new LedgerTransaction(
            request.TenantId,
            $"Stripe Payout: {request.Reference}",
            $"PO-{request.Reference}");

        transaction.AddEntry(bankAcc.Id, request.Amount, 0);

        // Credit Stripe Cash (Asset Decreases)
        transaction.AddEntry(stripeCashAcc.Id, 0, request.Amount);

        transaction.Post();

        _dbContext.LedgerTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Recorded Payout {Reference} of {Amount} {Currency} for Tenant {TenantId}",
            request.Reference, request.Amount, request.Currency, request.TenantId);

        return Result.Success();
    }

    private async Task<LedgerAccount> GetOrCreateAccountAsync(
        Guid tenantId,
        string code,
        string name,
        LedgerAccountType type,
        string currency,
        CancellationToken ct)
    {
        var acc = await _dbContext.LedgerAccounts
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Code == code, ct);

        if (acc == null)
        {
            acc = new LedgerAccount(tenantId, code, name, type, LedgerAccountRole.None, currency, true);
            _dbContext.LedgerAccounts.Add(acc);
            await _dbContext.SaveChangesAsync(ct);
        }

        return acc;
    }
}
