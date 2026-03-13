namespace Cobryx.Domain.Lending.Enums;

public enum DelinquencyStage
{
    Current,    // 0 days past due
    Early,      // Typically 1-30 days
    Moderate,   // Typically 31-60 days
    Severe,     // Typically 61-90 days
    Default,    // Typically 90+ days
    WriteOff    // Typically 120+ days or manual
}
