using Cobryx.Domain.ValueObjects;

namespace Cobryx.Application.Common.Interfaces;

/// <summary>
/// Thin wrapper over the Stripe SDK. Infrastructure-only implementation.
/// </summary>
public interface IStripeService
{
    Task<string> CreateCustomerAsync(string email, string tenantName, CancellationToken ct = default);

    Task<string> CreateCheckoutSessionAsync(string stripeCustomerId, string stripePriceId,
        Guid tenantId, int trialDays, string? successUrl = null, string? cancelUrl = null, CancellationToken ct = default);

    Task<string> CreateBillingPortalSessionAsync(string stripeCustomerId, string? returnUrl = null, CancellationToken ct = default);

    Task<StripeSubscriptionState> GetSubscriptionStateAsync(string stripeSubscriptionId, CancellationToken ct = default);

    Task<(string PaymentIntentId, string ClientSecret)> CreatePaymentIntentAsync(
        Money amount,
        Dictionary<string, string> metadata,
        string? destinationAccountId = null,
        decimal? applicationFeeAmount = null,
        CancellationToken ct = default);

    Task<string> CreateConnectOnboardingLinkAsync(string stripeAccountId, string returnUrl, string refreshUrl, CancellationToken ct = default);

    Task<string> CreateConnectAccountAsync(string email, string businessName, CancellationToken ct = default);

    Task<(bool ChargesEnabled, bool PayoutsEnabled, bool DetailsSubmitted)> GetConnectAccountStatusAsync(string stripeAccountId, CancellationToken ct = default);

    Task<string> GetPaymentIntentClientSecretAsync(string paymentIntentId, CancellationToken ct = default);

    Task<string> GetPaymentIntentStatusAsync(string paymentIntentId, CancellationToken ct = default);

    Task<(decimal Available, decimal Pending)> GetBalanceAsync(string? stripeAccountId = null, CancellationToken ct = default);
}

/// <summary>
/// Represents the current state of a Stripe subscription, used for sync.
/// </summary>
public record StripeSubscriptionState(
    string SubscriptionId,
    string Status,
    string PriceId,
    DateTime? TrialEnd,
    DateTime CurrentPeriodEnd);
