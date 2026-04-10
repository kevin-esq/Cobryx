using Cobryx.Application.Lending.Services;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;

using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Lending
{
    /// <summary>
    /// Engine responsible for distributing payment amounts across loan balances according to priority.
    /// </summary>
    /// <remarks>
    /// This engine follows a strict "Waterfall" allocation strategy where funds are applied
    /// sequentially to Fees, then Interest, and finally Principal (by default).
    /// </remarks>
    public partial class PaymentAllocationEngine(ILogger<PaymentAllocationEngine> logger) : IPaymentAllocationEngine
    {
        public Task<LoanPaymentAllocation> AllocateAsync(Loan loan, decimal paymentAmount, Guid paymentId,
            CancellationToken ct = default)
        {
            if (paymentAmount <= 0)
            {
                throw new DomainException(DomainErrorCode.Loans.InvalidPaymentAmount);
            }

            LogAllocatingPayment(logger, paymentAmount, loan.Id);

            var remaining = paymentAmount;

            var principalApplied = 0m;
            var interestApplied = 0m;
            var feesApplied = 0m;

            PaymentApplicationPolicy policy = loan.Agreement.PaymentApplicationPolicy
                                              ?? throw new DomainException(DomainErrorCode.Loans.InvalidLateFeePolicy);

            IEnumerable<PaymentApplicationType> priorityOrder = policy.GetPriorityOrder();

            foreach (PaymentApplicationType category in priorityOrder)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var outstanding = category switch
                {
                    PaymentApplicationType.LateFees => loan.OutstandingFees,
                    PaymentApplicationType.Interest => loan.OutstandingInterest,
                    PaymentApplicationType.Principal => loan.OutstandingPrincipal,
                    _ => 0m
                };

                var applied = Math.Min(outstanding, remaining);

                switch (category)
                {
                    case PaymentApplicationType.LateFees:
                        feesApplied = applied;
                        break;
                    case PaymentApplicationType.Interest:
                        interestApplied = applied;
                        break;
                    case PaymentApplicationType.Principal:
                        principalApplied = applied;
                        break;
                }

                remaining -= applied;
            }

            var allocation = new LoanPaymentAllocation(
                loan.TenantId,
                loan.Id,
                paymentId,
                principalApplied,
                interestApplied,
                feesApplied,
                remaining,
                0
            );

            return Task.FromResult(allocation);
        }
    }
}
