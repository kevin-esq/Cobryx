using Cobryx.Domain.Shared;
using Cobryx.Application.Common.Interfaces;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Lending.Commands.CloseLoan;

/// <summary>
/// Command to formally close a loan once all balances are paid.
/// </summary>
[TenantScoped]
public record CloseLoanCommand(Guid LoanId) : IRequest<Result>, IRequiresTenant;
