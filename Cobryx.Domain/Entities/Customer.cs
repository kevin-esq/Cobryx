using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public class Customer : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string FullName { get; private set; }
    public string Phone { get; private set; } // Primary ID for many small businesses
    public string? Address { get; private set; }
    public string? City { get; private set; }
    public string? ExternalReference { get; private set; } // INE, CURP, etc.
    public string? Notes { get; private set; }
    public int TrustScore { get; private set; } // 0-100
    public string? PhotoUrl { get; private set; }
    public bool IsActive { get; private set; }

    // Private ctor for EF
    private Customer() { }

    public Customer(Guid tenantId, string fullName, string phone, string? address, string? externalReference)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("FullName is required.", nameof(fullName));
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Phone is required.", nameof(phone));

        TenantId = tenantId;
        FullName = fullName;
        Phone = phone;
        Address = address;
        ExternalReference = externalReference;
        TrustScore = 80; // Default trust score
        IsActive = true;
    }

    public void UpdateProfile(string fullName, string phone, string? address, string? city, string? photoUrl)
    {
        if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("FullName is required.", nameof(fullName));
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Phone is required.", nameof(phone));

        FullName = fullName;
        Phone = phone;
        Address = address;
        City = city;
        PhotoUrl = photoUrl;
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
