using Cobryx.Domain.Common;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Exceptions;

namespace Cobryx.Domain.Entities;

public class Customer : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string FullName => $"{FirstName} {LastName}";
    public string Phone { get; private set; }
    public string Email { get; private set; }
    public Address? Address { get; private set; }
    public IdentityDocument? Document { get; private set; }
    public string? Notes { get; private set; }
    public int TrustScore { get; private set; }
    public string? StripeCustomerId { get; private set; }
    public string? DefaultPaymentMethodId { get; private set; }
    public bool HasSavedPaymentMethod { get; private set; }
    public bool AutoPayEnabled { get; private set; }
    public string? PhotoUrl { get; private set; }
    public bool IsActive { get; private set; }

    public virtual ICollection<Credit> Credits { get; private set; } = new List<Credit>();

    public Customer()
    {
        FirstName = null!;
        LastName = null!;
        Phone = null!;
        Email = null!;
    }

    public Customer(Guid tenantId, string firstName, string lastName, string phone, string email, Address? address, IdentityDocument? document)
    {
        if (tenantId == Guid.Empty) throw new DomainException(DomainErrorCode.Common.TenantIdRequired);
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException(DomainErrorCode.Customer.FirstNameRequired);
        if (string.IsNullOrWhiteSpace(lastName)) throw new DomainException(DomainErrorCode.Customer.LastNameRequired);
        if (string.IsNullOrWhiteSpace(phone)) throw new DomainException(DomainErrorCode.Customer.PhoneRequired);
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException(DomainErrorCode.Customer.EmailRequired);

        TenantId = tenantId;
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        Email = email;
        Address = address;
        Document = document;
        TrustScore = 80;
        IsActive = true;
        AutoPayEnabled = false;
        HasSavedPaymentMethod = false;
    }

    public void SetStripeCustomerId(string customerId)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new DomainException(DomainErrorCode.Common.ReasonRequired); // Reuse or use specific error

        StripeCustomerId = customerId;
        UpdateTimestamp();
    }

    public void SetDefaultPaymentMethod(string paymentMethodId)
    {
        if (string.IsNullOrWhiteSpace(paymentMethodId))
            throw new DomainException(DomainErrorCode.Common.ReasonRequired);

        DefaultPaymentMethodId = paymentMethodId;
        HasSavedPaymentMethod = true;
        UpdateTimestamp();
    }

    public void ToggleAutoPay(bool enabled)
    {
        AutoPayEnabled = enabled;
        UpdateTimestamp();
    }

    public void UpdateProfile(string firstName, string lastName, string phone, string email, Address? address, string? photoUrl)
    {
        UpdateDetails(firstName, lastName, phone, email, address, Document);
        PhotoUrl = photoUrl;
    }

    public void UpdateDetails(string firstName, string lastName, string phone, string email, Address? address, IdentityDocument? document)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new DomainException(DomainErrorCode.Customer.FirstNameRequired);
        if (string.IsNullOrWhiteSpace(lastName)) throw new DomainException(DomainErrorCode.Customer.LastNameRequired);
        if (string.IsNullOrWhiteSpace(phone)) throw new DomainException(DomainErrorCode.Customer.PhoneRequired);
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException(DomainErrorCode.Customer.EmailRequired);

        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        Email = email;
        Address = address;
        Document = document;
        UpdateTimestamp();
    }

    public void AdjustTrustScore(int delta)
    {
        TrustScore = Math.Clamp(TrustScore + delta, 0, 100);
        UpdateTimestamp();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }
}
