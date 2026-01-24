using Cobryx.Domain.Common;
using Concordia;

namespace Cobryx.Application.Customers.Commands.Create;

public record CreateCustomerCommand(
    Guid TenantId,
    string FullName,
    string Phone,
    string? Address = null,
    string? ExternalReference = null
) : IRequest<Result<Guid>>;
