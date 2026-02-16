using System;

namespace Cobryx.Api.Contracts.V1.Subscriptions;

/// <summary>
/// Status of the current tenant's subscription.
/// </summary>
public record SubscriptionStatusDto(
    string Status,
    string PlanName,
    string Tier,
    int MaxInvoices,
    int MaxUsers,
    DateTime? TrialEndsAtUtc,
    DateTime? GracePeriodEndsAtUtc,
    bool HasStripeCustomer
)
{
    /// <summary>The active status of the subscription (e.g., Active, PastDue).</summary>
    /// <example>Active</example>
    public string Status { get; init; } = Status;

    /// <summary>Human-readable name of the plan.</summary>
    /// <example>Pro Monthly</example>
    public string PlanName { get; init; } = PlanName;

    /// <summary>The technical tier (Starter, Pro, Enterprise).</summary>
    /// <example>Pro</example>
    public string Tier { get; init; } = Tier;

    /// <summary>Maximum number of invoices allowed per month.</summary>
    /// <example>500</example>
    public int MaxInvoices { get; init; } = MaxInvoices;

    /// <summary>Maximum number of users allowed in the tenant.</summary>
    /// <example>10</example>
    public int MaxUsers { get; init; } = MaxUsers;

    /// <summary>Date when the trial expires, if any.</summary>
    /// <example>null</example>
    public DateTime? TrialEndsAtUtc { get; init; } = TrialEndsAtUtc;

    /// <summary>Date when the grace period expires, if any.</summary>
    /// <example>null</example>
    public DateTime? GracePeriodEndsAtUtc { get; init; } = GracePeriodEndsAtUtc;

    /// <summary>Whether the tenant has a linked Stripe customer.</summary>
    /// <example>true</example>
    public bool HasStripeCustomer { get; init; } = HasStripeCustomer;
}

/// <summary>
/// Public contract for initiating a Stripe Checkout session.
/// </summary>
public record CreateCheckoutRequest(
    Guid PlanId,
    string? SuccessUrl = null,
    string? CancelUrl = null
)
{
    /// <summary>The unique ID of the subscription plan.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid PlanId { get; init; } = PlanId;

    /// <summary>Optional URL to redirect to after successful payment.</summary>
    /// <example>https://app.cobryx.mx/success</example>
    public string? SuccessUrl { get; init; } = SuccessUrl;

    /// <summary>Optional URL to redirect to if payment is cancelled.</summary>
    /// <example>https://app.cobryx.mx/cancel</example>
    public string? CancelUrl { get; init; } = CancelUrl;
}

/// <summary>
/// Public contract for initiating a Stripe Billing Portal session.
/// </summary>
public record CreatePortalRequest(
    string? ReturnUrl = null
)
{
    /// <summary>Optional URL to redirect to when leaving the Stripe billing portal.</summary>
    /// <example>https://app.cobryx.mx/billing</example>
    public string? ReturnUrl { get; init; } = ReturnUrl;
}

/// <summary>
/// Contains the URL to redirect the user to Stripe.
/// </summary>
public record CheckoutUrlDto(
    string Url
)
{
    /// <summary>The Stripe session or portal URL.</summary>
    /// <example>https://checkout.stripe.com/pay/cs_test_123</example>
    public string Url { get; init; } = Url;
}

/// <summary>
/// Public representation of a subscription plan.
/// </summary>
public record PlanDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int MaxInvoices,
    int MaxUsers,
    string Tier,
    int TrialDays
)
{
    /// <summary>Unique identifier of the plan.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; init; } = Id;

    /// <summary>Human-readable name.</summary>
    /// <example>Pro Monthly</example>
    public string Name { get; init; } = Name;

    /// <summary>Plan benefits and features.</summary>
    /// <example>Up to 500 invoices, 10 users, priority support.</example>
    public string Description { get; init; } = Description;

    /// <summary>Monthly price.</summary>
    /// <example>499.00</example>
    public decimal Price { get; init; } = Price;

    /// <summary>Currency code (ISO 4217).</summary>
    /// <example>MXN</example>
    public string Currency { get; init; } = Currency;

    /// <summary>Invoice limit per month.</summary>
    /// <example>500</example>
    public int MaxInvoices { get; init; } = MaxInvoices;

    /// <summary>User limit.</summary>
    /// <example>10</example>
    public int MaxUsers { get; init; } = MaxUsers;

    /// <summary>Technical tier.</summary>
    /// <example>Pro</example>
    public string Tier { get; init; } = Tier;

    /// <summary>Number of days included in the trial.</summary>
    /// <example>14</example>
    public int TrialDays { get; init; } = TrialDays;
}
