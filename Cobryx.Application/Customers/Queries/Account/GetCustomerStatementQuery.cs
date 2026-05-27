using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Customers.Queries.Account
{
    [TenantScoped]
public record GetCustomerStatementQuery(Guid CustomerId, Guid TenantId) : IRequest<Result<CustomerStatementDto>>, IRequiresTenant;

    public record CustomerStatementDto(
        Guid CustomerId,
        string CustomerName,
        DateTime StatementDate,
        decimal TotalOutstandingBalance,
        List<StatementLoanDto> Loans,
        List<StatementTransactionDto> Transactions);

    public record StatementLoanDto(
        Guid Id,
        string LoanNumber,
        decimal OriginalAmount,
        decimal PrincipalBalance,
        decimal InterestBalance,
        decimal LateFeeBalance,
        string Status,
        DateTime? MaturityDate);

    public record StatementTransactionDto(
        DateTime Date,
        string Description,
        decimal Amount,
        string Reference);

    public class GetCustomerStatementHandler(ICobryxDbContext dbContext, IClock clock) : IRequestHandler<GetCustomerStatementQuery, Result<CustomerStatementDto>>
    {
        public async Task<Result<CustomerStatementDto>> Handle(GetCustomerStatementQuery request, CancellationToken cancellationToken)
        {
            Customer? customer = await dbContext.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.TenantId == request.TenantId, cancellationToken);

            if (customer == null)
            {
                return Result.Failure<CustomerStatementDto>(DomainErrorCode.Customer.NotFound);
            }

            List<Loan> loans = await dbContext.Loans
                .AsNoTracking()
                .Where(l => l.CustomerId == request.CustomerId && l.TenantId == request.TenantId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync(cancellationToken);

            var loanIds = loans.Select(l => l.Id).ToList();

            List<LedgerTransaction> transactions = await dbContext.LedgerTransactions
                .AsNoTracking()
                .Include(t => t.Entries)
                .Where(t => t.LoanId.HasValue && loanIds.Contains(t.LoanId.Value) && t.IsPosted)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(cancellationToken);

            var totalBalance = transactions
                .SelectMany(t => t.Entries)
                .Where(e => dbContext.LedgerAccounts.Any(a => a.Id == e.AccountId && (a.Code == "1210" || a.Code == "4010" || a.Code == "4020")))
                .Sum(e => e.Debit - e.Credit);

            var loanDtos = loans.Select(l => new StatementLoanDto(
                Id: l.Id,
                LoanNumber: l.LoanNumber,
                OriginalAmount: l.OriginalPrincipal,
                PrincipalBalance: l.CurrentPrincipalBalance,
                InterestBalance: l.CurrentInterestBalance,
                LateFeeBalance: l.CurrentLateFeeBalance,
                Status: l.Status.ToString(),
                MaturityDate: l.NextPaymentDueDate
            )).ToList();

            var transactionDtos = transactions.Select(t =>
            {
                var cashImpact = t.Entries.FirstOrDefault(e => dbContext.LedgerAccounts.Any(a => a.Id == e.AccountId && a.Code == "1010"))?.Debit ?? 0;
                if (t.IsReversal)
                {
                    cashImpact = -t.Entries.Where(e => dbContext.LedgerAccounts.Any(a => a.Id == e.AccountId && a.Code == "1010")).Sum(e => e.Credit - e.Debit);
                }

                return new StatementTransactionDto(
                    Date: t.CreatedAt,
                    Description: t.Description,
                    Amount: cashImpact,
                    Reference: t.ReferenceId ?? string.Empty
                );
            }).ToList();

            return Result.Success(new CustomerStatementDto(
                CustomerId: customer.Id,
                CustomerName: $"{customer.FirstName} {customer.LastName}",
                StatementDate: clock.UtcNow,
                TotalOutstandingBalance: totalBalance,
                Loans: loanDtos,
                Transactions: transactionDtos
            ));
        }
    }
}
