namespace Cobryx.Application.Lending.Services;

/// <summary>
/// Orchestrates daily delinquency evaluation and risk escalation.
/// Run daily after the Accrual Engine.
/// </summary>
public interface ICollectionsEngine
{
    /// <summary>
    /// Evaluates all active loans for the given date, identifying DPD and delinquency stages.
    /// Triggers automated late fees and write-offs based on tenant policies.
    /// </summary>
    Task RunDailyEvaluationAsync(DateTime today, CancellationToken ct = default);

    /// <summary>
    /// Forces an evaluation for a specific loan (e.g. after a payment).
    /// </summary>
    Task EvaluateLoanAsync(Guid loanId, DateTime today, CancellationToken ct = default);
}
