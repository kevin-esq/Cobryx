using Cobryx.Domain.Lending;


namespace Cobryx.Application.Lending.Services;

public interface ILoanAccrualEngine
{
    /// <summary>
    /// Processes daily accruals for all active loans up to the specified date.
    /// Handles catch-ups for system downtime using the LastAccrualDate watermark.
    /// </summary>
    public Task RunDailyAccrualAsync(DateTime accrualDate, CancellationToken ct = default);

    /// <summary>
    /// Catch-up specific logic for a single loan up to targetDate.
    /// Usually called by payment services before allocation.
    /// </summary>
    public Task<int> ProcessLoanAccrualAsync(Loan loan, DateTime targetDate, CancellationToken ct = default);
}
