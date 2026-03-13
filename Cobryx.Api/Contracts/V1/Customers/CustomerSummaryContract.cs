using System.ComponentModel.DataAnnotations;

namespace Cobryx.Api.Contracts.V1.Customers;

/// <summary>
/// Professional summary of a customer record.
/// </summary>
public record CustomerSummaryContract(
    [Required] Guid Id,
    [Required] string FirstName,
    [Required] string LastName,
    [Required] string FullName,
    [Required] string Phone,
    string? DocumentType,
    string? DocumentNumber,
    string? City,
    string? State,
    bool IsActive
)
{
    /// <summary>Unique identifier for the customer record.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; init; } = Id;

    /// <summary>Customer's first name.</summary>
    /// <example>Jane</example>
    public string FirstName { get; init; } = FirstName;

    /// <summary>Customer's last name.</summary>
    /// <example>Smith</example>
    public string LastName { get; init; } = LastName;

    /// <summary>Customer's full legal name.</summary>
    /// <example>Jane Smith</example>
    public string FullName { get; init; } = FullName;

    /// <summary>Customer's primary contact phone number.</summary>
    /// <example>+525512345678</example>
    public string Phone { get; init; } = Phone;

    /// <summary>The type of identity document provided (RFC, CURP, INE).</summary>
    /// <example>INE</example>
    public string? DocumentType { get; init; } = DocumentType;

    /// <summary>The identifier value on the document.</summary>
    /// <example>SIMJ800101HDFLRS01</example>
    public string? DocumentNumber { get; init; } = DocumentNumber;

    /// <summary>The city of residence.</summary>
    /// <example>CDMX</example>
    public string? City { get; init; } = City;

    /// <summary>The state or province of residence.</summary>
    /// <example>Ciudad de México</example>
    public string? State { get; init; } = State;

    /// <summary>Indicates if the customer is currently enabled for operations.</summary>
    /// <example>true</example>
    public bool IsActive { get; init; } = IsActive;
}
