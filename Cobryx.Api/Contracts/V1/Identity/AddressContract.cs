using System.ComponentModel.DataAnnotations;

namespace Cobryx.Api.Contracts.V1.Identity;

/// <summary>
/// A reusable address structure.
/// </summary>
public record AddressContract(
    [Required] string Street,
    [Required] string HouseNumber,
    string? ApartmentNumber,
    string? Neighborhood,
    [Required] string City,
    [Required] string State,
    [Required] string PostalCode,
    string? Country = "Mexico"
)
{
    /// <summary>
    /// Formats the address components into a single line string.
    /// </summary>
    public string ToFormattedString()
    {
        var parts = new List<string> { $"{Street} {HouseNumber}" };
        if (!string.IsNullOrWhiteSpace(ApartmentNumber))
            parts.Add(ApartmentNumber);
        if (!string.IsNullOrWhiteSpace(Neighborhood))
            parts.Add(Neighborhood);
        if (!string.IsNullOrWhiteSpace(PostalCode))
            parts.Add(PostalCode);
        if (!string.IsNullOrWhiteSpace(City))
            parts.Add(City);
        if (!string.IsNullOrWhiteSpace(State))
            parts.Add(State);

        return string.Join(", ", parts);
    }
}
