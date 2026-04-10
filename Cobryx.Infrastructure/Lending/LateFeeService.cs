using Cobryx.Application.Lending.Services;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;

using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Lending
{
    public partial class LateFeeService(ILogger<LateFeeService> logger) : ILateFeeService
    {
        public decimal AssessLateFee(Loan loan, DateTime date)
        {
            LateFeePolicy? policy = loan.Agreement.LateFeePolicy;
            if (policy == null || !policy.IsActive)
            {
                return 0;
            }

            loan.UpdateFinancialRiskStatus(date);

            var dpd = loan.FinancialDaysPastDue;
            if (dpd <= policy.GracePeriodDays)
            {
                return 0;
            }

            if (policy.Type == LateFeeType.Fixed && dpd != policy.GracePeriodDays + 1)
            {
                return 0;
            }

            if (loan.AccruedCharges.Any(c => c.Type == ChargeType.LateFee && c.AccrualDate.Date == date.Date))
            {
                return 0;
            }

            var fee = policy.CalculateLateFee(dpd, loan.OutstandingPrincipal);

            if (fee > 0)
            {
                var charge = new AccruedCharge(loan.Id, ChargeType.LateFee, fee, date);
                loan.AddAccruedCharge(charge);
                LogLateFeeAssessed(logger, fee, loan.Id, dpd);
            }

            return fee;
        }
    }
}
