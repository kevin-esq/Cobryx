using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public class PaymentMethod : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string Code { get; private set; }
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }

    private PaymentMethod() 
    {
        Name = null!;
        Code = null!;
    }

    public PaymentMethod(Guid tenantId, string name, string code, string? description = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Payment method name is required.");
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Payment method code is required.");

        TenantId = tenantId;
        Name = name;
        Code = code.ToUpperInvariant();
        Description = description;
        IsActive = true;
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
        UpdateTimestamp();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    public static class Constants
    {
        public const string Cash = "CASH";
        public const string BankTransfer = "BANK_TRANSFER";
        public const string CreditCard = "CREDIT_CARD";
    }
}
