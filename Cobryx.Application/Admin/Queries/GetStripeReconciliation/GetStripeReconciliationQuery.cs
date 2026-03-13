using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Admin.Queries.GetStripeReconciliation;

public record GetStripeReconciliationQuery(Guid? TenantId = null) : IRequest<Result<StripeReconciliationDto>>;

public record StripeReconciliationDto(
    decimal LedgerCashBalance,
    decimal StripeAvailableBalance,
    decimal StripePendingBalance,
    decimal TotalStripeBalance,
    decimal Discrepancy,
    string Status,
    string? StripeAccountId);

public class GetStripeReconciliationHandler : IRequestHandler<GetStripeReconciliationQuery, Result<StripeReconciliationDto>>
{
    private readonly ICobryxDbContext _dbContext;
    private readonly IStripeService _stripeService;

    public GetStripeReconciliationHandler(ICobryxDbContext dbContext, IStripeService stripeService)
    {
        _dbContext = dbContext;
        _stripeService = stripeService;
    }

    public async Task<Result<StripeReconciliationDto>> Handle(GetStripeReconciliationQuery request, CancellationToken ct)
    {
        // 1. Resolve Target Tenant and Stripe Account
        var tenantId = request.TenantId ?? CobryxDefaults.PlatformTenantId;
        string? stripeAccountId = null;

        if (tenantId != CobryxDefaults.PlatformTenantId)
        {
            var tenant = await _dbContext.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tenantId, ct);
            if (tenant == null) return Result.Failure<StripeReconciliationDto>(DomainErrorCode.Tenant.NotFound);
            stripeAccountId = tenant.StripeAccountId;

            if (string.IsNullOrEmpty(stripeAccountId))
                return Result.Failure<StripeReconciliationDto>(DomainErrorCode.Common.GeneralError); // "Tenant has no Stripe Account"
        }

        // 2. Fetch Ledger Balance (Account 1010 - Cash)
        var cashAccount = await _dbContext.LedgerAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Code == "1010", ct);

        if (cashAccount == null)
            return Result.Failure<StripeReconciliationDto>(DomainErrorCode.Common.GeneralError); // "Cash account not found"

        var ledgerBalance = await _dbContext.LedgerEntries
            .AsNoTracking()
            .Where(e => e.AccountId == cashAccount.Id)
            .SumAsync(e => e.Debit - e.Credit, ct);

        // 3. Fetch Stripe Actual Balance
        var (available, pending) = await _stripeService.GetBalanceAsync(stripeAccountId, ct);
        var totalStripe = available + pending;

        // 4. Calculate Discrepancy
        var discrepancy = ledgerBalance - totalStripe;
        var status = Math.Abs(discrepancy) < 0.01m ? "SYNCED" : "DISCREPANCY";

        return Result.Success(new StripeReconciliationDto(
            LedgerCashBalance: ledgerBalance,
            StripeAvailableBalance: available,
            StripePendingBalance: pending,
            TotalStripeBalance: totalStripe,
            Discrepancy: discrepancy,
            Status: status,
            StripeAccountId: stripeAccountId
        ));
    }
}
