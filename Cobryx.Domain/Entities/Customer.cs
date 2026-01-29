using Cobryx.Domain.Common;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Entities;

public class Customer : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string FullName => $"{FirstName} {LastName}";
    public string Phone { get; private set; }
    public Address? Address { get; private set; }
    public IdentityDocument? Document { get; private set; }
    public string? Notes { get; private set; }
    public int TrustScore { get; private set; } // 0-100
    public string? PhotoUrl { get; private set; }
    public bool IsActive { get; private set; }

    public virtual ICollection<Credit> Credits { get; private set; } = new List<Credit>();


    private Customer()
    {
        FirstName = null!;
        LastName = null!;
        Phone = null!;
    }

    public Customer(Guid tenantId, string firstName, string lastName, string phone, Address? address, IdentityDocument? document)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("FirstName is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("LastName is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Phone is required.", nameof(phone));

        TenantId = tenantId;
        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
        Address = address;
        Document = document;
        TrustScore = 80;
        IsActive = true;
    }

    public void UpdateProfile(string firstName, string lastName, string phone, Address? address, string? photoUrl)
    {
        UpdateDetails(firstName, lastName, phone, address, Document);
        PhotoUrl = photoUrl;
    }

    public void UpdateDetails(string firstName, string lastName, string phone, Address? address, IdentityDocument? document)
    {
        if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentException("FirstName is required.", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName)) throw new ArgumentException("LastName is required.", nameof(lastName));
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Phone is required.", nameof(phone));

        FirstName = firstName;
        LastName = lastName;
        Phone = phone;
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
