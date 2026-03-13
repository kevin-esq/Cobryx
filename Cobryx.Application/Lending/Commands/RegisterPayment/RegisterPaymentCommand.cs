using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Lending.Commands.RegisterPayment;

/// <summary>
/// Command to register a payment against a loan and apply it to its components.
/// </summary>
public record RegisterPaymentCommand(
    Guid LoanId,
    decimal Amount,
    Guid PaymentMethodId,
    DateTime PaidAt,
    string? Reference = null,
    string? Notes = null
) : IRequest<Result<PaymentResultDto>>;

public record PaymentResultDto(
    Guid PaymentId,
    decimal AppliedAmount,
    decimal RemainingLoanBalance,
    decimal ExcessCredit
);
