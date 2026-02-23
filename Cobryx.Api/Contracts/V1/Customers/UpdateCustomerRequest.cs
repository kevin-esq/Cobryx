
namespace Cobryx.Api.Contracts.V1.Customers;

/// <summary>
/// Public contract for updating an existing customer.
/// </summary>
public record UpdateCustomerRequest(
    string FirstName,
    string LastName,
    string Phone,
    string Email,
    AddressContract? Address = null,
    IdentityDocumentContract? Document = null
)
{
    /// <summary>Customer's first name.</summary>
    /// <example>Jane</example>
    public string FirstName { get; init; } = FirstName;

    /// <summary>Customer's last name.</summary>
    /// <example>Smith</example>
    public string LastName { get; init; } = LastName;

    /// <summary>Customer's primary contact phone number.</summary>
    /// <example>+525512345678</example>
    public string Phone { get; init; } = Phone;

    /// <summary>Customer's primary contact email address.</summary>
    /// <example>jane.smith@example.com</example>
    public string Email { get; init; } = Email;

    /// <summary>Optional physical address details.</summary>
    public AddressContract? Address { get; init; } = Address;

    /// <summary>Optional identity document details.</summary>
    public IdentityDocumentContract? Document { get; init; } = Document;
}
