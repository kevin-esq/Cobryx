using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

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
        Money amount,
        Dictionary<string, string> metadata,
        string? destinationAccountId = null,
        decimal? applicationFeeAmount = null,
        CancellationToken ct = default)
    {
        var service = new PaymentIntentService();
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount.Amount * 100),
            Currency = amount.Currency.ToLowerInvariant(),
            Metadata = metadata,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            }
        };

        if (!string.IsNullOrEmpty(destinationAccountId))
        {
            options.TransferData = new PaymentIntentTransferDataOptions
            {
                Destination = destinationAccountId,
            };

            if (applicationFeeAmount.HasValue)
            {
                options.ApplicationFeeAmount = (long)(applicationFeeAmount.Value * 100);
            }
        }

        var intent = await service.CreateAsync(options, cancellationToken: ct);

        _logger.LogInformation(
            "Stripe PaymentIntent created: {IntentId} for {Amount} {Currency} (Connect: {IsConnect})",
            intent.Id, amount.Amount, amount.Currency, !string.IsNullOrEmpty(destinationAccountId));

        return (intent.Id, intent.ClientSecret);
    }

    public async Task<string> CreateConnectOnboardingLinkAsync(string stripeAccountId, string returnUrl, string refreshUrl, CancellationToken ct = default)
    {
        var service = new AccountLinkService();
        var options = new AccountLinkCreateOptions
        {
            Account = stripeAccountId,
            RefreshUrl = refreshUrl,
            ReturnUrl = returnUrl,
            Type = "account_onboarding",
        };

        var link = await service.CreateAsync(options, cancellationToken: ct);
        return link.Url;
    }

    public async Task<string> CreateConnectAccountAsync(string email, string businessName, CancellationToken ct = default)
    {
        var service = new AccountService();
        var options = new AccountCreateOptions
        {
            Type = "express",
            Email = email,
            BusinessProfile = new AccountBusinessProfileOptions
            {
                Name = businessName,
            },
            Capabilities = new AccountCapabilitiesOptions
            {
                CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
            }
        };

        var account = await service.CreateAsync(options, cancellationToken: ct);
        return account.Id;
    }

    public async Task<(bool ChargesEnabled, bool PayoutsEnabled, bool DetailsSubmitted)> GetConnectAccountStatusAsync(string stripeAccountId, CancellationToken ct = default)
    {
        var service = new AccountService();
        var account = await service.GetAsync(stripeAccountId, cancellationToken: ct);

        return (
            account.ChargesEnabled,
            account.PayoutsEnabled,
            account.DetailsSubmitted
        );
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

    public async Task<(decimal Available, decimal Pending)> GetBalanceAsync(string? stripeAccountId = null, CancellationToken ct = default)
    {
        var service = new BalanceService();
        var requestOptions = new RequestOptions();
        if (!string.IsNullOrEmpty(stripeAccountId))
        {
            requestOptions.StripeAccount = stripeAccountId;
        }

        var balance = await service.GetAsync(requestOptions, cancellationToken: ct);

        // Summing across all currency balances (defaulting to primary or total in USD equivalent if multi-currency)
        var available = balance.Available.Sum(b => b.Amount) / 100m;
        var pending = balance.Pending.Sum(b => b.Amount) / 100m;

        return (available, pending);
    }

    public async Task<string> CreateSetupIntentAsync(string customerId, CancellationToken ct = default)
    {
        var service = new SetupIntentService();
        var intent = await service.CreateAsync(new SetupIntentCreateOptions
        {
            Customer = customerId,
            PaymentMethodTypes = new List<string> { "card" },
            Usage = "off_session", // Critical for AutoPay/Scheduled charges
        }, cancellationToken: ct);

        return intent.ClientSecret;
    }

    public async Task AttachPaymentMethodAsync(string customerId, string paymentMethodId, CancellationToken ct = default)
    {
        var service = new PaymentMethodService();
        await service.AttachAsync(paymentMethodId, new PaymentMethodAttachOptions
        {
            Customer = customerId,
        }, cancellationToken: ct);

    }

    public async Task<List<StripePaymentMethodDto>> ListPaymentMethodsAsync(string customerId, CancellationToken ct = default)
    {
        var service = new PaymentMethodService();
        var options = new PaymentMethodListOptions
        {
            Customer = customerId,
            Type = "card",
        };

        var paymentMethods = await service.ListAsync(options, cancellationToken: ct);

        return paymentMethods.Select(pm => new StripePaymentMethodDto(
            Id: pm.Id,
            Brand: pm.Card.Brand,
            Last4: pm.Card.Last4,
            ExpMonth: (short)pm.Card.ExpMonth,
            ExpYear: (short)pm.Card.ExpYear,
            IsDefault: false
        )).ToList();
    }

    public async Task<string> ChargeSavedPaymentMethodAsync(
        string customerId,
        string paymentMethodId,
        decimal amount,
        string currency,
        string description,
        string? stripeAccountId = null,
        string? idempotencyKey = null,
        string? lastCursor = null,
        CancellationToken ct = default)
    {
        var service = new PaymentIntentService();
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100),
            Currency = currency.ToLowerInvariant(),
            Customer = customerId,
            PaymentMethod = paymentMethodId,
            Confirm = true,
            OffSession = true,
            Description = description,
        };

        var requestOptions = new RequestOptions
        {
            IdempotencyKey = idempotencyKey
        };

        if (!string.IsNullOrEmpty(stripeAccountId))
        {
            requestOptions.StripeAccount = stripeAccountId;
        }

        var intent = await service.CreateAsync(options, requestOptions, cancellationToken: ct);
        return intent.Id;
    }

    public async Task<List<StripePaymentIntentDto>> ListPaymentIntentsAsync(
        DateTime from,
        DateTime to,
        string? stripeAccountId = null,
        string? startingAfter = null,
        CancellationToken ct = default)
    {
        var service = new PaymentIntentService();
        var options = new PaymentIntentListOptions
        {
            Created = new DateRangeOptions
            {
                GreaterThanOrEqual = from,
                LessThanOrEqual = to
            },
            Limit = 100,
            StartingAfter = startingAfter
        };

        var requestOptions = new RequestOptions();
        if (!string.IsNullOrEmpty(stripeAccountId))
        {
            requestOptions.StripeAccount = stripeAccountId;
        }

        var intents = await service.ListAsync(options, requestOptions, cancellationToken: ct);

        return intents.Data.Select(i => new StripePaymentIntentDto(
            i.Id,
            i.Amount,
            i.Currency,
            i.Status,
            i.Created,
            i.Metadata ?? new Dictionary<string, string>()
        )).ToList();
    }

    public async Task<List<StripeBalanceTransactionDto>> ListBalanceTransactionsAsync(
        DateTime from,
        DateTime to,
        string? stripeAccountId = null,
        string? startingAfter = null,
        CancellationToken ct = default)
    {
        var service = new BalanceTransactionService();
        var options = new BalanceTransactionListOptions
        {
            Created = new DateRangeOptions
            {
                GreaterThanOrEqual = from,
                LessThanOrEqual = to
            },
            Limit = 100,
            StartingAfter = startingAfter,
            Expand = new List<string> { "data.source" }
        };

        var requestOptions = new RequestOptions();
        if (!string.IsNullOrEmpty(stripeAccountId))
        {
            requestOptions.StripeAccount = stripeAccountId;
        }

        var transactions = await service.ListAsync(options, requestOptions, cancellationToken: ct);

        return transactions.Data.Select(t => new StripeBalanceTransactionDto(
            t.Id,
            t.Amount,
            t.Fee,
            t.Net,
            t.Currency,
            t.Type,
            t.ReportingCategory,
            t.Status,
            t.Created,
            t.AvailableOn,
            t.SourceId,
            new Dictionary<string, string>()
        )).ToList();
    }
}
