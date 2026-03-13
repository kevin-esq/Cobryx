using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

namespace Cobryx.Application.Customers.Commands.Create;

public record CreateCustomerCommand(
    Guid TenantId,
    string FirstName,
    string LastName,
    string Phone,
    string Email,
    Address? Address = null,
    IdentityDocument? Document = null
) : IRequest<Result<Guid>>;
