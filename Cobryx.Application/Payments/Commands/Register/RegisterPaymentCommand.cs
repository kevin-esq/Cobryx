using Cobryx.Domain.Common;
using Concordia;

namespace Cobryx.Application.Payments.Commands.Register;

public record RegisterPaymentCommand(
    Guid TenantId,
    Guid CreditId,
    Guid PaymentMethodId,
    decimal Amount,
    string Currency,
    DateTime PaymentDate,
    string? Reference = null,
    string? Notes = null
) : IRequest<Result<Guid>>;
