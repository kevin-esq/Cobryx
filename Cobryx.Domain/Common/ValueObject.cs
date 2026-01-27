using System.Collections.Generic;
using System.Linq;

namespace Cobryx.Domain.Common;

public abstract record ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public virtual bool Equals(ValueObject? other)
    {
        if (other == null || other.GetType() != GetType())
            return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }
}
