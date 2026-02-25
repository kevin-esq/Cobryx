using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Lending.Enums;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Admin.Queries.GetFinancialMetrics;

public record GetFinancialMetricsQuery(Guid? TenantId = null) : IRequest<Result<FinancialMetricsDto>>;

public record FinancialMetricsDto(
    decimal TotalPortfolioPrincipal,
    decimal PAR30Principal,
    decimal PAR90Principal,
    decimal TotalChargedOffPrincipal,
    decimal TotalRecoveries,
    decimal RecoveryRatePercentage,
    decimal PAR30Percentage,
    decimal PAR90Percentage,
    int TotalActiveLoans);

public class GetFinancialMetricsHandler : IRequestHandler<GetFinancialMetricsQuery, Result<FinancialMetricsDto>>
{
    private readonly ICobryxDbContext _dbContext;

    public GetFinancialMetricsHandler(ICobryxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<FinancialMetricsDto>> Handle(GetFinancialMetricsQuery request, CancellationToken ct)
    {
        // 1. BANK-GRADE: Base Filter
        var loansQuery = _dbContext.Loans
            .AsNoTracking()
            .Where(l => l.Status == LoanStatus.Active);

        if (request.TenantId.HasValue)
        {
            loansQuery = loansQuery.Where(l => l.TenantId == request.TenantId.Value);
        }

        var activeLoans = await loansQuery.ToListAsync(ct);
        var loanIds = activeLoans.Select(l => l.Id).ToList();

        // 2. Fetch Ledger Principal Balances for these loans
        // We only count account '1210' (Principal)
        var principalEntries = await _dbContext.LedgerEntries
            .AsNoTracking()
            .Where(e => loanIds.Contains(e.TransactionId) || _dbContext.LedgerTransactions.Where(t => loanIds.Contains(t.LoanId ?? Guid.Empty)).Select(t => t.Id).Contains(e.TransactionId))
            .Join(_dbContext.LedgerAccounts.Where(a => a.Code == "1210"), e => e.AccountId, a => a.Id, (e, a) => e)
            .ToListAsync(ct);

        // Map principal by LoanId
        var loanBalances = activeLoans.Select(loan =>
        {
            var txIds = _dbContext.LedgerTransactions.Where(t => t.LoanId == loan.Id && t.IsPosted).Select(t => t.Id).ToList();
            var principal = principalEntries.Where(e => txIds.Contains(e.TransactionId)).Sum(e => e.Debit - e.Credit);
            return new { LoanId = loan.Id, Dpd = loan.FinancialDaysPastDue, Principal = principal };
        }).ToList();

        var totalPortfolio = loanBalances.Sum(x => x.Principal);
        var par30 = loanBalances.Where(x => x.Dpd > 30).Sum(x => x.Principal);
        var par90 = loanBalances.Where(x => x.Dpd > 90).Sum(x => x.Principal);

        // 3. Recovery and Charge-Off Metrics (Lifetime)
        var recoveryQuery = _dbContext.LedgerEntries
            .AsNoTracking()
            .Join(_dbContext.LedgerAccounts.Where(a => a.Code == "4030"), e => e.AccountId, a => a.Id, (e, a) => e);

        var chargeOffQuery = _dbContext.LedgerEntries
            .AsNoTracking()
            .Join(_dbContext.LedgerAccounts.Where(a => a.Code == "5010"), e => e.AccountId, a => a.Id, (e, a) => e);

        if (request.TenantId.HasValue)
        {
            recoveryQuery = recoveryQuery.Where(e => _dbContext.LedgerAccounts.Where(a => a.TenantId == request.TenantId.Value).Select(a => a.Id).Contains(e.AccountId));
            chargeOffQuery = chargeOffQuery.Where(e => _dbContext.LedgerAccounts.Where(a => a.TenantId == request.TenantId.Value).Select(a => a.Id).Contains(e.AccountId));
        }

        var totalRecoveries = await recoveryQuery.SumAsync(e => e.Credit - e.Debit, ct);
        var totalLosses = await chargeOffQuery.SumAsync(e => e.Debit - e.Credit, ct);

        decimal recoveryRate = totalLosses > 0 ? Math.Round((totalRecoveries / totalLosses) * 100, 2) : 0;
        decimal par30Pct = totalPortfolio > 0 ? Math.Round((par30 / totalPortfolio) * 100, 2) : 0;
        decimal par90Pct = totalPortfolio > 0 ? Math.Round((par90 / totalPortfolio) * 100, 2) : 0;

        return Result.Success(new FinancialMetricsDto(
            TotalPortfolioPrincipal: totalPortfolio,
            PAR30Principal: par30,
            PAR90Principal: par90,
            TotalChargedOffPrincipal: totalLosses,
            TotalRecoveries: totalRecoveries,
            RecoveryRatePercentage: recoveryRate,
            PAR30Percentage: par30Pct,
            PAR90Percentage: par90Pct,
            TotalActiveLoans: activeLoans.Count
        ));
    }
}
