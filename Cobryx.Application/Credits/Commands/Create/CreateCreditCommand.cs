using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.ValueObjects;
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
