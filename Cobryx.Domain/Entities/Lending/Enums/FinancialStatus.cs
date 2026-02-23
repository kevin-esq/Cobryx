namespace Cobryx.Domain.Entities.Lending.Enums;

/// <summary>
/// Professional risk classification based on financial behavior and days past due (DPD).
/// Separate from contractual LoanStatus.
/// </summary>
public enum FinancialStatus
{
    /// <summary>
    /// Loan is up to date or within the grace period (0-3 DPD).
    /// </summary>
    Current = 1,

    /// <summary>
    /// Initial delinquency (4-30 DPD).
    /// </summary>
    Late = 2,

    /// <summary>
    /// Sustained delinquency (31-90 DPD).
    /// </summary>
    Delinquent = 3,

    /// <summary>
    /// Significant risk of loss (91-180 DPD). Non-performing.
    /// </summary>
    Default = 4,

    /// <summary>
    /// Accounting loss. Written off from assets (181+ DPD or policy).
    /// </summary>
    ChargedOff = 5,

    /// <summary>
    /// Payments received after a Charge-Off.
    /// </summary>
    Recovered = 6
}
