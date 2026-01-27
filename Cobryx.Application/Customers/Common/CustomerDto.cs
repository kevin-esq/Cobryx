using Cobryx.Domain.ValueObjects;

namespace Cobryx.Application.Customers.Common;

public record CustomerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Phone,
    Address? Address,
    IdentityDocument? Document,
    int ActiveCreditsCount,
    DateTime CreatedAt);
