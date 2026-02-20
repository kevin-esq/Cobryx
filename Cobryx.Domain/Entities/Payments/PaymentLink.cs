using System.Security.Cryptography;
using System.Text;
using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Exceptions;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Events.Payments;

namespace Cobryx.Domain.Entities.Payments;

public class PaymentLink : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? LoanId { get; private set; }
    public Money AmountSnapshot { get; private set; }
    public Money PaidAmount { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string Salt { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public bool SingleUse { get; private set; }
    public int MaxAttempts { get; private set; }
    public int AttemptCount { get; private set; }
    public PaymentLinkStatus Status { get; private set; }
    public string? ExternalReference { get; private set; }
    public string? StripePaymentIntentId { get; private set; }
    public DateTime? LastReminderSentAt { get; private set; }
    public int ReminderCount { get; private set; }

    private PaymentLink() { }

    public PaymentLink(
        Guid tenantId,
        Guid customerId,
        Money amount,
        string rawToken,
        DateTime expiresAt,
        string serverSecret,
        Guid? loanId = null,
        string? externalReference = null,
        bool singleUse = true,
        int maxAttempts = 5)
    {
        TenantId = tenantId;
        CustomerId = customerId;
        LoanId = loanId;
        AmountSnapshot = amount;
        PaidAmount = Money.Zero(amount.Currency);
        ExpiresAt = expiresAt;
        ExternalReference = externalReference;
        SingleUse = singleUse;
        MaxAttempts = maxAttempts;
        Status = PaymentLinkStatus.Active;
        AttemptCount = 0;
        Salt = Guid.NewGuid().ToString("N");

        // Note: Raw token is hashed immediately and not stored
        SetToken(rawToken, serverSecret);
    }

    public void SetToken(string rawToken, string serverSecret)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            throw new DomainException(DomainErrorCode.Common.GeneralError);

        TokenHash = ComputeHmac(rawToken, serverSecret);
    }

    public bool ValidateToken(string rawToken, string serverSecret)
    {
        if (Status is PaymentLinkStatus.Expired or PaymentLinkStatus.Cancelled or PaymentLinkStatus.Failed)
            return false;

        if (AttemptCount >= MaxAttempts)
        {
            Status = PaymentLinkStatus.Failed;
            UpdateTimestamp();
            return false;
        }

        var isValid = TokenHash == ComputeHmac(rawToken, serverSecret);
        
        if (!isValid)
        {
            AttemptCount++;
            if (AttemptCount >= MaxAttempts)
                Status = PaymentLinkStatus.Failed;
            
            UpdateTimestamp();
        }

        return isValid;
    }

    public void MarkAsProcessing(string stripePaymentIntentId)
    {
        if (Status != PaymentLinkStatus.Active)
            throw new DomainException(DomainErrorCode.PaymentLink.InvalidStatus);

        if (DateTime.UtcNow > ExpiresAt)
        {
            Status = PaymentLinkStatus.Expired;
            throw new DomainException(DomainErrorCode.PaymentLink.Expired);
        }

        Status = PaymentLinkStatus.Processing;
        StripePaymentIntentId = stripePaymentIntentId;
        UpdateTimestamp();
    }

    public void MarkAsPaid(Money amount)
    {
        if (Status != PaymentLinkStatus.Processing && Status != PaymentLinkStatus.Active)
            throw new DomainException(DomainErrorCode.PaymentLink.InvalidStatus);

        if (amount.Currency != AmountSnapshot.Currency || amount.Amount != AmountSnapshot.Amount)
            throw new DomainException(DomainErrorCode.ValueObjects.CurrencyMismatch);

        PaidAmount = amount;
        Status = PaymentLinkStatus.Paid;
        
        // Finalize
        UpdateTimestamp();
    }

    public void Expire()
    {
        if (Status == PaymentLinkStatus.Active || Status == PaymentLinkStatus.Processing)
        {
            Status = PaymentLinkStatus.Expired;
            UpdateTimestamp();
        }
    }

    public void RecordReminderSent()
    {
        LastReminderSentAt = DateTime.UtcNow;
        ReminderCount++;
        UpdateTimestamp();
    }

    private string ComputeHmac(string rawToken, string serverSecret)
    {
        // Composite Key: secret + salt
        var key = Encoding.UTF8.GetBytes(serverSecret + Salt);
        using var hmac = new HMACSHA256(key);
        
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = hmac.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
