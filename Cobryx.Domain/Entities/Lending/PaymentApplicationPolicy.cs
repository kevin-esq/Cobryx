using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Lending.Enums;
using Cobryx.Domain.Exceptions;

namespace Cobryx.Domain.Entities.Lending;

/// <summary>
/// Configurable policy for payment allocation order.
/// </summary>
public class PaymentApplicationPolicy : BaseEntity, ITenantEntity
{
    private static readonly PaymentApplicationType[] StandardOrder =
    [
        PaymentApplicationType.LateFees,
        PaymentApplicationType.Interest,
        PaymentApplicationType.Principal
    ];

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public PaymentApplicationMode Mode { get; private set; }
    public string? ApplicationOrderJson { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDefault { get; private set; }

    private PaymentApplicationPolicy() { }

    public static PaymentApplicationPolicy CreateStandard(
        Guid tenantId,
        string name,
        bool isDefault = false)
    {
        if (tenantId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(DomainErrorCode.Common.EntityNameRequired);

        return new PaymentApplicationPolicy
        {
            TenantId = tenantId,
            Name = name,
            Mode = PaymentApplicationMode.Standard,
            IsActive = true,
            IsDefault = isDefault
        };
    }

    public static PaymentApplicationPolicy CreateCustom(
        Guid tenantId,
        string name,
        PaymentApplicationType[] applicationOrder,
        bool isDefault = false)
    {
        if (tenantId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(DomainErrorCode.Common.EntityNameRequired);
        if (applicationOrder == null || applicationOrder.Length == 0)
            throw new DomainException(DomainErrorCode.Common.GeneralError);

        var json = System.Text.Json.JsonSerializer.Serialize(applicationOrder);

        return new PaymentApplicationPolicy
        {
            TenantId = tenantId,
            Name = name,
            Mode = PaymentApplicationMode.Custom,
            ApplicationOrderJson = json,
            IsActive = true,
            IsDefault = isDefault
        };
    }

    public PaymentApplicationType[] GetApplicationOrder()
    {
        if (Mode == PaymentApplicationMode.Standard)
            return StandardOrder;

        if (string.IsNullOrWhiteSpace(ApplicationOrderJson))
            return StandardOrder;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<PaymentApplicationType[]>(ApplicationOrderJson)
                ?? StandardOrder;
        }
        catch
        {
            return StandardOrder;
        }
    }

    public void SetAsDefault()
    {
        IsDefault = true;
        UpdateTimestamp();
    }

    public void ClearDefault()
    {
        IsDefault = false;
        UpdateTimestamp();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    public void Activate()
    {
        IsActive = true;
        UpdateTimestamp();
    }
}
