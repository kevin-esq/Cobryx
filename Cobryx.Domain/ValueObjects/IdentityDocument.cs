using Cobryx.Domain.Shared;
using Cobryx.Domain.Shared.Enums;


namespace Cobryx.Domain.ValueObjects;

public record IdentityDocument(DocumentType Type, string Value) : ValueObject
{
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Value;
    }
}
