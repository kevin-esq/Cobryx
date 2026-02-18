using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Subscriptions.Common;

namespace Cobryx.IntegrationTests.Fakes;

public class MockStripeService : IStripeService
{
    private readonly IClock _clock;

    public MockStripeService(IClock clock)
    {
        _clock = clock;
    }

    public Task<string> CreateCustomerAsync(string email, string tenantName, CancellationToken ct = default)
    {
        return Task.FromResult("cus_fake_123");
    }

    public Task<string> CreateCheckoutSessionAsync(
        string stripeCustomerId,
        string stripePriceId,
        Guid tenantId,
        int trialDays,
        string? successUrl = null,
        string? cancelUrl = null,
        CancellationToken ct = default)
    {
        return Task.FromResult("https://checkout.stripe.com/fake_session");
    }

    public Task<string> CreateBillingPortalSessionAsync(string stripeCustomerId, string? returnUrl = null, CancellationToken ct = default)
    {
        return Task.FromResult("https://billing.stripe.com/fake_portal");
    }

    public Task<StripeSubscriptionState> GetSubscriptionStateAsync(string stripeSubscriptionId, CancellationToken ct = default)
    {
        return Task.FromResult(new StripeSubscriptionState(
            stripeSubscriptionId,
            StripeConstants.Statuses.Active,
            "price_fake_123",
            null,
            _clock.UtcNow.AddMonths(1)));
    }
}
