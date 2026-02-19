using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Entities;

public class SubscriptionPlan : BaseEntity, IAggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public int MaxInvoices { get; private set; }
    public int MaxUsers { get; private set; }
    public int MaxLoans { get; private set; }
    public bool IsActive { get; private set; }

    // Stripe integration
    public string? StripePriceId { get; private set; }
    public int TrialDays { get; private set; }
    public PlanTier Tier { get; private set; }

    private SubscriptionPlan()
    {
        Name = null!;
        Description = null!;
        Price = null!;
    }

    public SubscriptionPlan(string name, string description, Money price, int maxInvoices, int maxUsers, int maxLoans = 0,
        PlanTier tier = PlanTier.Free, int trialDays = 0, string? stripePriceId = null)
    {
        Name = name;
        Description = description;
        Price = price;
        MaxInvoices = maxInvoices;
        MaxUsers = maxUsers;
        MaxLoans = maxLoans;
        IsActive = true;
        Tier = tier;
        TrialDays = trialDays;
        StripePriceId = stripePriceId;
    }

    public void SetStripePriceId(string stripePriceId)
    {
        StripePriceId = stripePriceId;
        UpdateTimestamp();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }
}
