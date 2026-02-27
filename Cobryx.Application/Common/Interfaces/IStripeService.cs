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

    Task<string> CreateSetupIntentAsync(string customerId, CancellationToken ct = default);

    Task AttachPaymentMethodAsync(string customerId, string paymentMethodId, CancellationToken ct = default);

    Task<List<StripePaymentMethodDto>> ListPaymentMethodsAsync(string customerId, CancellationToken ct = default);

    Task<string> ChargeSavedPaymentMethodAsync(
        string customerId,
        string paymentMethodId,
        decimal amount,
        string currency,
        string description,
        string? stripeAccountId = null,
        string? idempotencyKey = null,
        string? lastCursor = null,
        CancellationToken ct = default);

    Task<List<StripePaymentIntentDto>> ListPaymentIntentsAsync(
        DateTime from,
        DateTime to,
        string? stripeAccountId = null,
        string? startingAfter = null,
        CancellationToken ct = default);

    Task<List<StripeBalanceTransactionDto>> ListBalanceTransactionsAsync(
        DateTime from,
        DateTime to,
        string? stripeAccountId = null,
        string? startingAfter = null,
        CancellationToken ct = default);
}

public record StripeBalanceTransactionDto(
    string Id,
    long Amount,
    long Fee,
    long Net,
    string Currency,
    string Type,
    string ReportingCategory,
    string Status,
    DateTime Created,
    DateTime AvailableOn,
    string? SourceId,
    Dictionary<string, string> Metadata);

public record StripePaymentIntentDto(
    string Id,
    long Amount,
    string Currency,
    string Status,
    DateTime Created,
    Dictionary<string, string> Metadata);

public record StripePaymentMethodDto(
    string Id,
    string Brand,
    string Last4,
    short ExpMonth,
    short ExpYear,
    bool IsDefault);

/// <summary>
/// Represents the current state of a Stripe subscription, used for sync.
/// </summary>
public record StripeSubscriptionState(
    string SubscriptionId,
    string Status,
    string PriceId,
    DateTime? TrialEnd,
    DateTime CurrentPeriodEnd);
