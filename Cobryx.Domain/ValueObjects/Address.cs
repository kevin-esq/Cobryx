using Cobryx.Domain.Shared;

namespace Cobryx.Domain.ValueObjects;

public record Address(
    string Street,
    string ExtNumber,
    string? IntNumber,
    string Neighborhood,
    string ZipCode,
    string City,
    string State) : ValueObject
{
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Street;
        yield return ExtNumber;
        yield return IntNumber;
        yield return Neighborhood;
        yield return ZipCode;
        yield return City;
        yield return State;
    }
}
