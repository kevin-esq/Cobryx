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
);
