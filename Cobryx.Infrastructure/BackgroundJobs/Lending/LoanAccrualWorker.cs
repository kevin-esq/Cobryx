using Cobryx.Application.Lending.Services;

using Hangfire;

using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs.Lending;

public class LoanAccrualWorker
{
    private readonly ILoanAccrualEngine _accrualEngine;
    private readonly ILogger<LoanAccrualWorker> _logger;

    public LoanAccrualWorker(ILoanAccrualEngine accrualEngine, ILogger<LoanAccrualWorker> logger)
    {
        _accrualEngine = accrualEngine;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("LoanAccrualWorker triggered at {Time}", DateTime.UtcNow);

        // We run accrual for today (accruing interest for the previous day usually, 
        // but our engine handles it using the LastAccrualDate watermark up to DateTime.UtcNow.Date)
        await _accrualEngine.RunDailyAccrualAsync(DateTime.UtcNow.Date);
    }
}
