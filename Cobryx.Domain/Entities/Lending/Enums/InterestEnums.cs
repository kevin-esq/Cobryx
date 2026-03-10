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

public enum DayCountBasis
{
    Actual365 = 1,
    Actual360 = 2,
    Thirty360 = 3
}
