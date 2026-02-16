namespace Cobryx.Api.Contracts.V1.Common;

/// <summary>
/// Public-facing address structure.
/// </summary>
public record AddressContract(
    string Street,
    string HouseNumber,
    string? ApartmentNumber = null,
    string? Neighborhood = null,
    string? PostalCode = null,
    string? City = null,
    string? State = null
)
{
    /// <summary>Street name or main thoroughfare.</summary>
    /// <example>Av. Insurgentes Sur</example>
    public string Street { get; init; } = Street;

    /// <summary>External building number.</summary>
    /// <example>123</example>
    public string HouseNumber { get; init; } = HouseNumber;

    /// <summary>Internal unit or apartment number.</summary>
    /// <example>402-B</example>
    public string? ApartmentNumber { get; init; } = ApartmentNumber;

    /// <summary>Neighborhood or district name.</summary>
    /// <example>Col. Del Valle</example>
    public string? Neighborhood { get; init; } = Neighborhood;

    /// <summary>Local postal code.</summary>
    /// <example>03100</example>
    public string? PostalCode { get; init; } = PostalCode;

    /// <summary>City or municipality name.</summary>
    /// <example>CDMX</example>
    public string? City { get; init; } = City;

    /// <summary>State, province or autonomous region.</summary>
    /// <example>Ciudad de México</example>
    public string? State { get; init; } = State;
}
