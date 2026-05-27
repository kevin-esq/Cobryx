using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Portal.Queries.GetCustomerPortalSummary
{
    [TokenScoped]
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

    public class GetCustomerPortalSummaryHandler(ICobryxDbContext dbContext, IClock clock) : IRequestHandler<GetCustomerPortalSummaryQuery, Result<CustomerPortalSummaryDto>>
    {
        public async Task<Result<CustomerPortalSummaryDto>> Handle(GetCustomerPortalSummaryQuery request, CancellationToken cancellationToken)
        {
            bool customerExists = await dbContext.Customers
                .AnyAsync(c => c.Id == request.CustomerId && c.TenantId == request.TenantId, cancellationToken);

            if (!customerExists)
            {
                return Result.Failure<CustomerPortalSummaryDto>(DomainErrorCode.Customer.NotFound);
            }

            var loans = await dbContext.Loans
                .Include(l => l.Installments)
                .Where(l => l.CustomerId == request.CustomerId && l.TenantId == request.TenantId && l.Status == LoanStatus.Active)
                .ToListAsync(cancellationToken);

            if (loans.Count == 0)
            {
                return Result.Success(new CustomerPortalSummaryDto(0, 0, null, [], []));
            }

            var loanIds = loans.Select(l => l.Id).ToList();

            var transactions = await dbContext.LedgerTransactions
                .Where(t => t.TenantId == request.TenantId && t.LoanId.HasValue && loanIds.Contains(t.LoanId.Value) && t.IsPosted)
                .OrderByDescending(t => t.EffectiveDate)
                .Take(20)
                .ToListAsync(cancellationToken);

            var transactionIds = transactions.Select(t => t.Id).ToList();
            var entries = await dbContext.LedgerEntries
                .Where(e => transactionIds.Contains(e.TransactionId))
                .ToListAsync(cancellationToken);

            decimal totalBalance = entries.Sum(e => e.Debit - e.Credit);

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
                .Where(i => i.Status != InstallmentStatus.Paid && i.DueDate >= clock.UtcNow.Date)
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
}
