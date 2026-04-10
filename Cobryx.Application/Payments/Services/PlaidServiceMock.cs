using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Accounting;

namespace Cobryx.Application.Payments.Services
{
    public interface IBankProviderService
    {
        public Task<List<BankMovement>> FetchRecentMovementsAsync(Guid tenantId, DateTime since,
            CancellationToken ct = default);
    }

    public class PlaidServiceMock(IClock clock) : IBankProviderService
    {
        public Task<List<BankMovement>> FetchRecentMovementsAsync(Guid tenantId, DateTime since,
            CancellationToken ct = default)
        {
            DateTime now = clock.UtcNow;
            List<BankMovement> movements =
            [
                new(
                    tenantId,
                    1000.00m,
                    "USD",
                    BankMovementDirection.Inbound,
                    now.AddDays(-1),
                    now.AddDays(-1),
                    "Plaid",
                    "plaid_trans_001",
                    "PAYOUT-STRIPE-001"),

                new(
                    tenantId,
                    500.00m,
                    "USD",
                    BankMovementDirection.Inbound,
                    now.AddDays(-2),
                    now.AddDays(-2),
                    "Plaid",
                    "plaid_trans_002",
                    "ST-002"),

                new(
                    tenantId,
                    1500.00m,
                    "USD",
                    BankMovementDirection.Inbound,
                    now.AddDays(-3),
                    now.AddDays(-3),
                    "Plaid",
                    "plaid_trans_003")
            ];

            return Task.FromResult(movements);
        }
    }
}
