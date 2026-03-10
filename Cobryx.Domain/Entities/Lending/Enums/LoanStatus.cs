namespace Cobryx.Domain.Entities.Lending.Enums;

public enum LoanStatus
{
    Draft = 1,
    Active = 2,
    Closed = 3,
    Cancelled = 4,
    WrittenOff = 5,
    Disputed = 6,
    Suspended = 7
}

public enum LegalStatus
{
    Active = 1,
    Restructured = 2,
    Defaulted = 3,
    Closed = 4,
    InLegal = 5,
    PaymentPlanActive = 6
}

/// <summary>
/// Risk classification based on days in arrears.
/// </summary>
public enum RiskStatus
{
    OnTime = 1,
    Late = 2,
    SevereLate = 3,
    Critical = 4
}

public enum CollectionStage
{
    None = 0,
    Friendly = 1,
    Hard = 2,
    Legal = 3
}
