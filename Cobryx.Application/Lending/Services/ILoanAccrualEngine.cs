using Cobryx.Domain.Entities.Lending;

namespace Cobryx.Application.Lending.Services;

public interface ILoanAccrualEngine
{
    /// <summary>
    /// Processes daily accruals for all active loans up to the specified date.
    /// Handles catch-ups for system downtime using the LastAccrualDate watermark.
    /// </summary>
    Task RunDailyAccrualAsync(DateTime accrualDate, CancellationToken ct = default);

    /// <summary>
    /// Catch-up specific logic for a single loan up to targetDate.
    /// Usually called by payment services before allocation.
    /// </summary>
    Task<int> ProcessLoanAccrualAsync(Cobryx.Domain.Entities.Lending.Loan loan, DateTime targetDate, CancellationToken ct = default);
}
