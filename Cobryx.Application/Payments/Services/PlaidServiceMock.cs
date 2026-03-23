using Cobryx.Domain.Accounting;

namespace Cobryx.Application.Payments.Services;

public interface IBankProviderService
{
    public Task<List<BankMovement>> FetchRecentMovementsAsync(Guid tenantId, DateTime since, CancellationToken ct = default);
}

public class PlaidServiceMock : IBankProviderService
{
    public Task<List<BankMovement>> FetchRecentMovementsAsync(Guid tenantId, DateTime since, CancellationToken ct = default)
    {
        var movements = new List<BankMovement>
        {
            new BankMovement(
                tenantId,
                1000.00m,
                "USD",
                BankMovementDirection.Inbound,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(-1),
                "Plaid",
                "plaid_trans_001",
                "PAYOUT-STRIPE-001"),

            new BankMovement(
                tenantId,
                500.00m,
                "USD",
                BankMovementDirection.Inbound,
                DateTime.UtcNow.AddDays(-2),
                DateTime.UtcNow.AddDays(-2),
                "Plaid",
                "plaid_trans_002",
                "ST-002"),

            new BankMovement(
                tenantId,
                1500.00m,
                "USD",
                BankMovementDirection.Inbound,
                DateTime.UtcNow.AddDays(-3),
                DateTime.UtcNow.AddDays(-3),
                "Plaid",
                "plaid_trans_003",
                null)
        };

        return Task.FromResult(movements);
    }
}
