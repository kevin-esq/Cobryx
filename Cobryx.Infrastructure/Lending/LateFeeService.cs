using Cobryx.Application.Lending.Services;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;

using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Lending;

public class LateFeeService : ILateFeeService
{
    private readonly ILogger<LateFeeService> _logger;

    public LateFeeService(ILogger<LateFeeService> logger)
    {
        _logger = logger;
    }

    public decimal AssessLateFee(Loan loan, DateTime date)
    {
        var policy = loan.Agreement?.LateFeePolicy;
        if (policy == null || !policy.IsActive) return 0;

        // Ensure DPD is calculated for the target accrual date
        loan.UpdateFinancialRiskStatus(date);

        var dpd = loan.FinancialDaysPastDue;
        if (dpd <= policy.GracePeriodDays) return 0;

        // Institutional rule: 
        // Fixed: apply only on the exact day grace period ends (dpd == grace + 1)
        // Daily: apply every day as long as dpd > grace

        if (policy.Type == LateFeeType.Fixed && dpd != policy.GracePeriodDays + 1)
        {
            return 0;
        }

        // Check if already assessed for this day to maintain idempotency
        if (loan.AccruedCharges.Any(c => c.Type == ChargeType.LateFee && c.AccrualDate.Date == date.Date))
        {
            return 0;
        }

        // policy.CalculateLateFee expects (effectiveDaysLate, balance)
        // Note: The policy logic in CalculateLateFee subtracts GracePeriodDays again, 
        // so we pass the raw dpd.
        var fee = policy.CalculateLateFee(dpd, loan.OutstandingPrincipal);

        if (fee > 0)
        {
            var charge = new AccruedCharge(loan.Id, ChargeType.LateFee, fee, date);
            loan.AddAccruedCharge(charge);
            _logger.LogInformation("Assessed Late Fee of {Amount} for Loan {LoanId} (DPD: {Dpd})", fee, loan.Id, dpd);
        }

        return fee;
    }
}
