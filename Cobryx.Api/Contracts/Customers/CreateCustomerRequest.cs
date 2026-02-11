using Cobryx.Api.Contracts.V1.Common;

namespace Cobryx.Api.Contracts.V1.Customers;

/// <summary>
/// Public contract for creating a new customer.
/// </summary>
public record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string Phone,
    AddressContract? Address = null,
    IdentityDocumentContract? Document = null
);
