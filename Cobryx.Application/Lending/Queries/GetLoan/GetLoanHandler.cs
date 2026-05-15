using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Lending.Queries.GetLoan;

public class GetLoanHandler : IRequestHandler<GetLoanQuery, Result<LoanDto>>
{
    public Task<Result<LoanDto>> Handle(GetLoanQuery request, CancellationToken cancellationToken)
    {
        // Placeholder for real DB retrieval logic.
        // In a real implementation, this would use a repository or Dapper.
        return Task.FromResult(Result.Failure<LoanDto>(DomainErrorCode.Common.EntityNotFound));
    }
}
