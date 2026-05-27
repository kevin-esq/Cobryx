using Cobryx.Domain.Shared;
using Cobryx.Application.Common.Interfaces;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Lending.Commands.RegisterPayment;

/// <summary>
/// Command to register a payment against a loan and apply it to its components.
/// </summary>
[TenantScoped]
public record RegisterPaymentCommand(
    Guid LoanId,
    decimal Amount,
    Guid PaymentMethodId,
    DateTime PaidAt,
    string? Reference = null,
    string? Notes = null
) : IRequest<Result<PaymentResultDto>>, IRequiresTenant;

public record PaymentResultDto(
    Guid PaymentId,
    decimal AppliedAmount,
    decimal RemainingLoanBalance,
    decimal ExcessCredit
);
