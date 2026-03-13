using System.ComponentModel.DataAnnotations;

namespace Cobryx.Api.Contracts.V1.Subscriptions;

/// <summary>
/// Data required to initiate a new Stripe Checkout session.
/// </summary>
public record CreateCheckoutSessionRequest(
    [Required] Guid PlanId,
    [Required] string SuccessUrl,
    [Required] string CancelUrl)
{
    /// <summary>The unique identifier of the subscription plan.</summary>
    /// <example>00000000-0000-0000-0000-000000000001</example>
    public Guid PlanId { get; init; } = PlanId;

    /// <summary>URL to redirect to after successful payment.</summary>
    /// <example>https://app.cobryx.mx/success</example>
    public string SuccessUrl { get; init; } = SuccessUrl;

    /// <summary>URL to redirect to if payment is cancelled.</summary>
    /// <example>https://app.cobryx.mx/cancel</example>
    public string CancelUrl { get; init; } = CancelUrl;
}

/// <summary>
/// Data required to initiate a Stripe Billing Portal session.
/// </summary>
public record CreatePortalSessionRequest(
    [Required] string ReturnUrl
)
{
    /// <summary>URL to redirect to when leaving the Stripe billing portal.</summary>
    /// <example>https://app.cobryx.mx/billing</example>
    public string ReturnUrl { get; init; } = ReturnUrl;
}

/// <summary>
/// Contains the URL to redirect the user to Stripe.
/// </summary>
public record CheckoutUrlResponse(
    string Url
)
{
    /// <summary>The Stripe session or portal URL.</summary>
    /// <example>https://checkout.stripe.com/pay/cs_test_123</example>
    public string Url { get; init; } = Url;
}
