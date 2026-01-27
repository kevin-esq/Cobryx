using Cobryx.Domain.Common;
using Cobryx.Domain.ValueObjects;
using Concordia;

namespace Cobryx.Application.Customers.Commands.Create;

public record CreateCustomerCommand(
    Guid TenantId,
    string FirstName,
    string LastName,
    string Phone,
    Address? Address = null,
    IdentityDocument? Document = null
) : IRequest<Result<Guid>>;
