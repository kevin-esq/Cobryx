using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Lending.Commands.CloseLoan;

/// <summary>
/// Command to formally close a loan once all balances are paid.
/// </summary>
public record CloseLoanCommand(Guid LoanId) : IRequest<Result>;
