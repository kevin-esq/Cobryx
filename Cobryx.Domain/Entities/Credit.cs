using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Entities;

public class Credit : BaseEntity, IAggregateRoot
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? ProductId { get; private set; } // Null if it's a direct cash loan
    
    public Money Principal { get; private set; }
    public decimal InterestRate { get; private set; }
    public InterestType InterestType { get; private set; }
    public PaymentFrequency Frequency { get; private set; }
    public int InstallmentsCount { get; private set; }
    
    public DateTime StartDate { get; private set; }
    public int GraceDays { get; private set; } // <--- New: Advanced flexibility
    public CreditStatus Status { get; private set; }

    private readonly List<Installment> _installments = new();
    public IReadOnlyCollection<Installment> Installments => _installments.AsReadOnly();

    private Credit() { }

    public Credit(
        Guid tenantId, 
        Guid customerId, 
        Money principal, 
        decimal interestRate, 
        InterestType interestType, 
        PaymentFrequency frequency, 
        int installmentsCount,
        int graceDays = 0, // <--- New parameter
        Guid? productId = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.");
        if (customerId == Guid.Empty) throw new ArgumentException("CustomerId is required.");
        if (installmentsCount <= 0) throw new ArgumentException("Installments count must be greater than zero.");

        TenantId = tenantId;
        CustomerId = customerId;
        Principal = principal;
        InterestRate = interestRate;
        InterestType = interestType;
        Frequency = frequency;
        InstallmentsCount = installmentsCount;
        GraceDays = graceDays;
        ProductId = productId;
        
        StartDate = DateTime.UtcNow;
        Status = CreditStatus.Active;
    }

    public void AddInstallments(IEnumerable<Installment> installments)
    {
        if (_installments.Any()) throw new InvalidOperationException("Installments already generated.");
        _installments.AddRange(installments);
    }
}
