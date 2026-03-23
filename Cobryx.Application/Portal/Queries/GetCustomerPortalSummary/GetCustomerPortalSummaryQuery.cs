using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Portal.Queries.GetCustomerPortalSummary;

public record GetCustomerPortalSummaryQuery(Guid CustomerId, Guid TenantId) : IRequest<Result<CustomerPortalSummaryDto>>;

public record CustomerPortalSummaryDto(
    decimal TotalOutstandingBalance,
    decimal NextPaymentAmount,
    DateTime? NextPaymentDueDate,
    List<PortalLoanSummaryDto> ActiveLoans,
    List<PortalTransactionDto> RecentTransactions);

public record PortalLoanSummaryDto(
    Guid LoanId,
    string LoanNumber,
    decimal PrincipalBalance,
    decimal InterestBalance,
    int Dpd,
    string Status);

public record PortalTransactionDto(
    Guid TransactionId,
    DateTime Date,
    string Description,
    decimal Amount,
    string Type,
    string? ReferenceId);

public class GetCustomerPortalSummaryHandler(ICobryxDbContext dbContext) : IRequestHandler<GetCustomerPortalSummaryQuery, Result<CustomerPortalSummaryDto>>
{
    private readonly ICobryxDbContext _dbContext = dbContext;

    public async Task<Result<CustomerPortalSummaryDto>> Handle(GetCustomerPortalSummaryQuery request, CancellationToken ct)
    {
        var customerExists = await _dbContext.Customers
            .AnyAsync(c => c.Id == request.CustomerId && c.TenantId == request.TenantId, ct);

        if (!customerExists)
        {
            return Result.Failure<CustomerPortalSummaryDto>(DomainErrorCode.Customer.NotFound);
        }

        var loans = await _dbContext.Loans
            .Include(l => l.Installments)
            .Where(l => l.CustomerId == request.CustomerId && l.TenantId == request.TenantId && l.Status == LoanStatus.Active)
            .ToListAsync(ct);

        if (loans.Count == 0)
        {
            return Result.Success(new CustomerPortalSummaryDto(0, 0, null, [], []));
        }

        var loanIds = loans.Select(l => l.Id).ToList();

        var transactions = await _dbContext.LedgerTransactions
            .Where(t => t.TenantId == request.TenantId && t.LoanId.HasValue && loanIds.Contains(t.LoanId.Value) && t.IsPosted)
            .OrderByDescending(t => t.EffectiveDate)
            .Take(20)
            .ToListAsync(ct);

        var transactionIds = transactions.Select(t => t.Id).ToList();
        var entries = await _dbContext.LedgerEntries
            .Where(e => transactionIds.Contains(e.TransactionId))
            .ToListAsync(ct);


        var totalBalance = entries.Sum(e => e.Debit - e.Credit);

        var loanSummaries = loans.Select(l => new PortalLoanSummaryDto(
            l.Id,
            l.LoanNumber,
            l.CurrentPrincipalBalance,
            l.CurrentInterestBalance,
            l.FinancialDaysPastDue,
            l.FinancialStatus.ToString()
        )).ToList();

        var recentTransactions = transactions
            .Where(t => t.Description.Contains("Payment") || t.IsReversal)
            .Select(t =>
            {
                var txEntries = entries.Where(e => e.TransactionId == t.Id).ToList();
                var amount = txEntries.Sum(e => e.Credit);

                return new PortalTransactionDto(
                    t.Id,
                    t.EffectiveDate,
                    t.IsReversal ? "Refund" : "Payment",
                    Math.Abs(txEntries.Sum(e => e.Debit - e.Credit)),
                    t.IsReversal ? "Refund" : "Payment",
                    t.ReferenceId
                );
            }).ToList();

        var nextInstallment = loans
            .SelectMany(l => l.Installments)
            .Where(i => i.Status != InstallmentStatus.Paid && i.DueDate >= DateTime.UtcNow.Date)
            .OrderBy(i => i.DueDate)
            .FirstOrDefault();

        return Result.Success(new CustomerPortalSummaryDto(
            totalBalance,
            nextInstallment?.TotalAmount.Amount ?? 0m,
            nextInstallment?.DueDate,
            loanSummaries,
            recentTransactions
        ));
    }
}
