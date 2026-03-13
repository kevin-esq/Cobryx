using Cobryx.Domain.Shared;

namespace Cobryx.Domain.ValueObjects;

/// <summary>
/// Status of a Tenant's Stripe Connect capabilities.
/// </summary>
public record TenantConnectCapability : ValueObject
{
    public bool ChargesEnabled { get; private set; }
    public bool PayoutsEnabled { get; private set; }
    public bool DetailsSubmitted { get; private set; }

    public TenantConnectCapability(bool chargesEnabled, bool payoutsEnabled, bool detailsSubmitted)
    {
        ChargesEnabled = chargesEnabled;
        PayoutsEnabled = payoutsEnabled;
        DetailsSubmitted = detailsSubmitted;
    }

    public static TenantConnectCapability NotStarted() => new(false, false, false);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ChargesEnabled;
        yield return PayoutsEnabled;
        yield return DetailsSubmitted;
    }
}
