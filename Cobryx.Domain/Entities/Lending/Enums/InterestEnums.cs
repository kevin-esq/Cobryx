namespace Cobryx.Domain.Entities.Lending.Enums;

public enum InterestType
{
    Explicit = 1,
    Implicit = 2  // From price difference (cash vs credit price)
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
