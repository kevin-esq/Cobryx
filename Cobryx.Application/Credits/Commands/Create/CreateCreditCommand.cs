using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Credits.Commands.Create;

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
) : IRequest<Result<Guid>>;
