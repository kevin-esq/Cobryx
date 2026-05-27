using Cobryx.Domain.Lending.Enums;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Credits.Commands.Create;

[TenantScoped]
public record CreateCreditCommand(
    Guid TenantId,
    Guid CustomerId,
    decimal Amount,
    string Currency,
    decimal InterestRate,
    InterestType InterestType,
    PaymentFrequency Frequency,
    int InstallmentsCount,
    int GraceDays = 0,
    Guid? ProductId = null
) : IRequest<Result<Guid>>, IRequiresTenant;
