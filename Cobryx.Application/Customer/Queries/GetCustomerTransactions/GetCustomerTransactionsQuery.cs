using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Customer.Queries.GetCustomerTransactions;

public record GetCustomerTransactionsQuery(Guid LoanId) : IRequest<Result<List<CustomerTransactionDto>>>;

public record CustomerTransactionDto(
    Guid Id,
    DateTime Date,
    string Description,
    decimal Amount,
    string Type,
    string Reference);

public class GetCustomerTransactionsHandler(ICobryxDbContext dbContext) : IRequestHandler<GetCustomerTransactionsQuery, Result<List<CustomerTransactionDto>>>
{
    private readonly ICobryxDbContext _dbContext = dbContext;

    public async Task<Result<List<CustomerTransactionDto>>> Handle(GetCustomerTransactionsQuery request, CancellationToken ct)
    {
        var transactions = await _dbContext.LedgerTransactions
            .AsNoTracking()
            .Where(t => t.LoanId == request.LoanId && t.IsPosted)
            .Include(t => t.Entries)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        var dtos = transactions.Select(t =>
        {
            var cashImpact = t.Entries.FirstOrDefault(e => _dbContext.LedgerAccounts.Any(a => a.Id == e.AccountId && a.Code == "1010"))?.Debit ?? 0;

            if (t.IsReversal)
            {
                cashImpact = -t.Entries.Where(e => _dbContext.LedgerAccounts.Any(a => a.Id == e.AccountId && a.Code == "1010")).Sum(e => e.Credit - e.Debit);
            }

            return new CustomerTransactionDto(
                Id: t.Id,
                Date: t.CreatedAt,
                Description: t.Description,
                Amount: cashImpact,
                Type: t.IsReversal ? "Reversal" : (t.Description.Contains("CHARGE-OFF") ? "ChargeOff" : "Payment"),
                Reference: t.ReferenceId ?? string.Empty
            );
        }).ToList();

        return Result.Success(dtos);
    }
}
