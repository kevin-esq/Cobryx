using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

namespace Cobryx.Application.Customers.Commands.Create;

// * TenantId parameter is IGNORED by handler for security.
// Tenant context is resolved via ITenantProvider.
// This parameter is deprecated and will be removed in future refactor.
[TenantScoped]
public record CreateCustomerCommand(
    Guid TenantId,
    string FirstName,
    string LastName,
    string Phone,
    string Email,
    Address? Address = null,
    IdentityDocument? Document = null
) : IRequest<Result<Guid>>, IRequiresTenant;
