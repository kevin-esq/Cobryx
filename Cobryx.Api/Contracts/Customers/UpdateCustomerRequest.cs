using Cobryx.Api.Contracts.V1.Common;

namespace Cobryx.Api.Contracts.V1.Customers;

/// <summary>
/// Public contract for updating an existing customer.
/// </summary>
public record UpdateCustomerRequest(
    string FirstName,
    string LastName,
    string Phone,
    AddressContract? Address = null,
    IdentityDocumentContract? Document = null
);
