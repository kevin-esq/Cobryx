using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Payments.Commands.Register;

// * TenantId parameter is IGNORED by handler.
// Tenant context is resolved via ITenantProvider for security.
// This parameter is deprecated and will be removed in future refactor.
[TenantScoped]
public record RegisterPaymentCommand(
    Guid TenantId,
    Guid CreditId,
    Guid PaymentMethodId,
    decimal Amount,
    string Currency,
    DateTime PaymentDate,
    string? Reference = null,
    string? Notes = null
) : IRequest<Result<Guid>>, IRequiresTenant;
