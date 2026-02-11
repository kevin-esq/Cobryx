namespace Cobryx.Domain.Entities.Lending.Enums;

public enum InterestOrigin
{
    Explicit = 1,
    Implicit = 2
}

public enum InterestMethod
{
    Simple = 1,
    Compound = 2
}

public enum CompoundingFrequency
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3
}
