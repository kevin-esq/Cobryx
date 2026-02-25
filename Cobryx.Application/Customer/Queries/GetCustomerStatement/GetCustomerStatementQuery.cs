using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Entities.Lending.Enums;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Customer.Queries.GetCustomerStatement;

public record GetCustomerStatementQuery(Guid CustomerId, Guid TenantId) : IRequest<Result<CustomerStatementDto>>;

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

public class GetCustomerStatementHandler : IRequestHandler<GetCustomerStatementQuery, Result<CustomerStatementDto>>
{
    private readonly ICobryxDbContext _dbContext;

    public GetCustomerStatementHandler(ICobryxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<CustomerStatementDto>> Handle(GetCustomerStatementQuery request, CancellationToken ct)
    {
        // 1. Fetch Customer Detail
        var customer = await _dbContext.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId && c.TenantId == request.TenantId, ct);

        if (customer == null)
            return Result.Failure<CustomerStatementDto>(DomainErrorCode.Customer.NotFound);

        // 2. Fetch Active and Recently Closed Loans
        var loans = await _dbContext.Loans
            .AsNoTracking()
            .Where(l => l.CustomerId == request.CustomerId && l.TenantId == request.TenantId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync(ct);

        var loanIds = loans.Select(l => l.Id).ToList();

        // 3. Fetch Ledger Transactions for these loans
        var transactions = await _dbContext.LedgerTransactions
            .AsNoTracking()
            .Include(t => t.Entries)
            .Where(t => t.LoanId.HasValue && loanIds.Contains(t.LoanId.Value) && t.IsPosted)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        // 4. Calculate Aggregate Metrics
        var totalBalance = transactions
            .SelectMany(t => t.Entries)
            .Where(e => _dbContext.LedgerAccounts.Any(a => a.Id == e.AccountId && (a.Code == "1210" || a.Code == "4010" || a.Code == "4020")))
            .Sum(e => e.Debit - e.Credit);

        // 5. Prepare DTOS
        var loanDtos = loans.Select(l => new StatementLoanDto(
            Id: l.Id,
            LoanNumber: l.LoanNumber,
            OriginalAmount: l.OriginalPrincipal,
            PrincipalBalance: l.CurrentPrincipalBalance,
            InterestBalance: l.CurrentInterestBalance,
            LateFeeBalance: l.CurrentLateFeeBalance,
            Status: l.Status.ToString(),
            MaturityDate: l.NextPaymentDueDate // Fallback for MVP
        )).ToList();

        var transactionDtos = transactions.Select(t =>
        {
            var cashImpact = t.Entries.FirstOrDefault(e => _dbContext.LedgerAccounts.Any(a => a.Id == e.AccountId && a.Code == "1010"))?.Debit ?? 0;
            if (t.IsReversal) cashImpact = -t.Entries.Where(e => _dbContext.LedgerAccounts.Any(a => a.Id == e.AccountId && a.Code == "1010")).Sum(e => e.Credit - e.Debit);

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
            StatementDate: DateTime.UtcNow,
            TotalOutstandingBalance: totalBalance,
            Loans: loanDtos,
            Transactions: transactionDtos
        ));
    }
}
