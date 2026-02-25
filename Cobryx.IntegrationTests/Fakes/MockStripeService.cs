using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Subscriptions.Common;
using Cobryx.Domain.ValueObjects;

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

    public Task<(string PaymentIntentId, string ClientSecret)> CreatePaymentIntentAsync(
        Money amount,
        Dictionary<string, string> metadata,
        string? destinationAccountId = null,
        decimal? applicationFeeAmount = null,
        CancellationToken ct = default)
    {
        return Task.FromResult(("pi_fake_123", "secret_fake_123"));
    }

    public Task<string> CreateConnectOnboardingLinkAsync(string stripeAccountId, string returnUrl, string refreshUrl, CancellationToken ct = default)
    {
        return Task.FromResult("https://connect.stripe.com/fake_onboarding");
    }

    public Task<string> CreateConnectAccountAsync(string email, string businessName, CancellationToken ct = default)
    {
        return Task.FromResult("acct_fake_123");
    }

    public Task<(bool ChargesEnabled, bool PayoutsEnabled, bool DetailsSubmitted)> GetConnectAccountStatusAsync(string stripeAccountId, CancellationToken ct = default)
    {
        return Task.FromResult((true, true, true));
    }

    public Task<string> GetPaymentIntentClientSecretAsync(string paymentIntentId, CancellationToken ct = default)
    {
        return Task.FromResult("secret_fake_123");
    }

    public Task<string> GetPaymentIntentStatusAsync(string paymentIntentId, CancellationToken ct = default)
    {
        return Task.FromResult("requires_payment_method");
    }
}
