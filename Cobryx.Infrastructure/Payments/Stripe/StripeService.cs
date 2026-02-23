using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Infrastructure.Configuration;
using Cobryx.Application.Common.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Cobryx.Infrastructure.Payments.Stripe;

public class StripeService : IStripeService
{
    private readonly StripeOptions _options;
    private readonly ILogger<StripeService> _logger;

    public StripeService(IOptions<StripeOptions> options, ILogger<StripeService> logger)
    {
        _options = options.Value;
        _logger = logger;
        StripeConfiguration.ApiKey = _options.SecretKey;
    }

    public async Task<string> CreateCustomerAsync(string email, string tenantName, CancellationToken ct = default)
    {
        var service = new CustomerService();
        var customer = await service.CreateAsync(new CustomerCreateOptions
        {
            Email = email,
            Name = tenantName,
            Metadata = new Dictionary<string, string>
            {
                { "source", "cobryx" }
            }
        }, cancellationToken: ct);

        _logger.LogInformation("Stripe Customer created: {CustomerId} for {Email}", customer.Id, email);
        return customer.Id;
    }

    public async Task<string> CreateCheckoutSessionAsync(
        string stripeCustomerId,
        string stripePriceId,
        Guid tenantId,
        int trialDays,
        string? successUrl = null,
        string? cancelUrl = null,
        CancellationToken ct = default)
    {
        var service = new SessionService();

        var options = new SessionCreateOptions
        {
            Customer = stripeCustomerId,
            Mode = "subscription",
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Price = stripePriceId,
                    Quantity = 1,
                }
            },
            SuccessUrl = (successUrl ?? _options.SuccessUrl) + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = cancelUrl ?? _options.CancelUrl,
            Metadata = new Dictionary<string, string>
            {
                { CobryxClaimTypes.TenantId, tenantId.ToString() }
            }
        };

        if (trialDays > 0)
        {
            options.SubscriptionData = new SessionSubscriptionDataOptions
            {
                TrialPeriodDays = trialDays,
            };
        }

        var session = await service.CreateAsync(options, cancellationToken: ct);

        _logger.LogInformation(
            "Checkout session created: {SessionId} for Tenant {TenantId}, Price {PriceId}",
            session.Id, tenantId, stripePriceId);

        return session.Url;
    }

    public async Task<string> CreateBillingPortalSessionAsync(string stripeCustomerId, string? returnUrl = null, CancellationToken ct = default)
    {
        var service = new global::Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(new global::Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = stripeCustomerId,
            ReturnUrl = returnUrl ?? _options.SuccessUrl,
        }, cancellationToken: ct);

        return session.Url;
    }

    public async Task<StripeSubscriptionState> GetSubscriptionStateAsync(string stripeSubscriptionId, CancellationToken ct = default)
    {
        var service = new SubscriptionService();
        var subscription = await service.GetAsync(stripeSubscriptionId, cancellationToken: ct);

        var firstItem = subscription.Items?.Data?.FirstOrDefault();
        var priceId = firstItem?.Price?.Id ?? string.Empty;

        var currentPeriodEnd = firstItem?.CurrentPeriodEnd ?? DateTime.UtcNow.AddMonths(1);

        return new StripeSubscriptionState(
            subscription.Id,
            subscription.Status!,
            priceId,
            subscription.TrialEnd,
            currentPeriodEnd);
    }

    public async Task<(string PaymentIntentId, string ClientSecret)> CreatePaymentIntentAsync(
        Domain.ValueObjects.Money amount,
        Dictionary<string, string> metadata,
        CancellationToken ct = default)
    {
        var service = new PaymentIntentService();
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount.Amount * 100), // Convert to cents
            Currency = amount.Currency.ToLowerInvariant(),
            Metadata = metadata,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            }
        };

        var intent = await service.CreateAsync(options, cancellationToken: ct);

        _logger.LogInformation(
            "Stripe PaymentIntent created: {IntentId} for {Amount} {Currency}",
            intent.Id, amount.Amount, amount.Currency);

        return (intent.Id, intent.ClientSecret);
    }

    public async Task<string> GetPaymentIntentClientSecretAsync(string paymentIntentId, CancellationToken ct = default)
    {
        var service = new PaymentIntentService();
        var intent = await service.GetAsync(paymentIntentId, cancellationToken: ct);
        return intent.ClientSecret;
    }

    public async Task<string> GetPaymentIntentStatusAsync(string paymentIntentId, CancellationToken ct = default)
    {
        var service = new PaymentIntentService();
        var intent = await service.GetAsync(paymentIntentId, cancellationToken: ct);
        return intent.Status;
    }
}
