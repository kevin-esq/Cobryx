namespace Cobryx.Domain.Entities.Lending.Enums;

public enum LoanStatus
{
    Pending = 1,
    Active = 2,
    Closed = 3,
    Defaulted = 4
}

public enum LegalStatus
{
    Active = 1,
    Restructured = 2,
    Defaulted = 3,
    Closed = 4
}

/// <summary>
/// Risk classification based on days in arrears.
/// </summary>
public enum RiskStatus
{
    OnTime = 1,
    Late = 2,        // 1-30 days
    SevereLate = 3,  // 31-60 days
    Critical = 4     // 60+ days
}

public enum CollectionStage
{
    None = 0,
    Friendly = 1,
    Hard = 2,
    Legal = 3
}
