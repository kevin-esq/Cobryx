using Cobryx.Domain.Common;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Entities;

public class SubscriptionPlan : BaseEntity, IAggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public Money Price { get; private set; }
    public int MaxInvoices { get; private set; }
    public int MaxUsers { get; private set; }
    public bool IsActive { get; private set; }

    private SubscriptionPlan()
    {
        Name = null!;
        Description = null!;
        Price = null!;
    }

    public SubscriptionPlan(string name, string description, Money price, int maxInvoices, int maxUsers)
    {
        Name = name;
        Description = description;
        Price = price;
        MaxInvoices = maxInvoices;
        MaxUsers = maxUsers;
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }
}
