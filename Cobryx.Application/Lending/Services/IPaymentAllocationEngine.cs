using Cobryx.Domain.Entities.Lending;
using System.Threading;
using System.Threading.Tasks;

namespace Cobryx.Application.Lending.Services;

public interface IPaymentAllocationEngine
{
    /// <summary>
    /// Deterministically allocates a payment amount across loan balances (Fees, Interest, Principal)
    /// based on the loan's PaymentApplicationPolicy.
    /// </summary>
    /// <param name="loan">The loan aggregate.</param>
    /// <param name="paymentAmount">The amount to allocate. Must be positive.</param>
    /// <param name="paymentId">The unique identifier of the payment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A plan describing the allocation distribution.</returns>
    Task<LoanPaymentAllocation> AllocateAsync(Loan loan, decimal paymentAmount, Guid paymentId, CancellationToken ct = default);
}
