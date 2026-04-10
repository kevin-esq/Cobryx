using System.Security.Cryptography;
using System.Text;

using Cobryx.Domain.Lending;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Payments;

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
    public int RecoveryAttemptCount { get; private set; }
    public DateTime? LastRecoveryAttemptAt { get; private set; }
    public DateTime? NextRecoveryAttemptAt { get; private set; }
    public DateTime? RecoveryDeadline { get; private set; }
    public int MaxRecoveryAttempts { get; private set; } = 5;
    public string? RecoveryFailureReason { get; private set; }
    public bool RecoveryInProgress { get; private set; }

    public virtual Customer Customer { get; private set; } = null!;
    public virtual Cobryx.Domain.Lending.Loan? Loan { get; private set; }

    private PaymentLink() { AmountSnapshot = null!; PaidAmount = null!; }

    public PaymentLink(
        Guid tenantId,
        Guid customerId,
        Money amount,
        string rawToken,
        DateTime expiresAt,
        string serverSecret,
        DateTime now,
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
        RecoveryDeadline = now.AddDays(14);

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

        var currentHash = ComputeHmac(rawToken, serverSecret);
        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(TokenHash),
            Encoding.UTF8.GetBytes(currentHash));

        if (!isValid)
        {
            AttemptCount++;
            if (AttemptCount >= MaxAttempts)
                Status = PaymentLinkStatus.Failed;

            UpdateTimestamp();
        }

        return isValid;
    }

    public void MarkAsProcessing(string stripePaymentIntentId, DateTime now)
    {
        if (Status != PaymentLinkStatus.Active)
            throw new DomainException(DomainErrorCode.PaymentLink.InvalidStatus);

        if (now > ExpiresAt)
        {
            Status = PaymentLinkStatus.Expired;
            throw new DomainException(DomainErrorCode.PaymentLink.Expired);
        }

        Status = PaymentLinkStatus.Processing;
        StripePaymentIntentId = stripePaymentIntentId;
        UpdateTimestamp(now);
    }

    public void MarkAsPaid(Money amount)
    {
        if (Status != PaymentLinkStatus.Processing && Status != PaymentLinkStatus.Active)
            throw new DomainException(DomainErrorCode.PaymentLink.InvalidStatus);

        if (amount.Currency != AmountSnapshot.Currency || amount.Amount != AmountSnapshot.Amount)
            throw new DomainException(DomainErrorCode.ValueObjects.CurrencyMismatch);

        PaidAmount = amount;
        Status = PaymentLinkStatus.Paid;

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

    public void ResetToActive()
    {
        if (Status == PaymentLinkStatus.Processing)
        {
            if (Status == PaymentLinkStatus.ManualReview)
                return;

            Status = PaymentLinkStatus.Active;
            UpdateTimestamp();
        }
    }

    public void RecordRecoveryFailure(string reason, DateTime now)
    {
        RecoveryAttemptCount++;
        LastRecoveryAttemptAt = now;
        RecoveryFailureReason = reason;

        var baseNextAttempt = RecoveryAttemptCount switch
        {
            1 => now.AddHours(1),
            2 => now.AddHours(8),
            3 => now.AddHours(24),
            4 => now.AddDays(3),
            5 => now.AddDays(7),
            _ => (DateTime?)null
        };

        if (baseNextAttempt.HasValue)
        {
            var seed = BitConverter.ToInt32(Id.ToByteArray(), 0);
            var random = new Random(seed + RecoveryAttemptCount);
            var jitterFactor = (random.NextDouble() * 0.2) - 0.1;

            var interval = baseNextAttempt.Value - now;
            NextRecoveryAttemptAt = baseNextAttempt.Value.AddTicks((long)(interval.Ticks * jitterFactor));
        }
        else
        {
            NextRecoveryAttemptAt = null;
        }

        if (RecoveryAttemptCount > MaxRecoveryAttempts || (RecoveryDeadline.HasValue && now > RecoveryDeadline))
        {
            Status = PaymentLinkStatus.ManualReview;
            NextRecoveryAttemptAt = null;
        }

        UpdateTimestamp(now);
    }

    public bool TryAcquireRecoveryLock()
    {
        if (RecoveryInProgress || Status == PaymentLinkStatus.Paid || Status == PaymentLinkStatus.ManualReview)
            return false;

        RecoveryInProgress = true;
        UpdateTimestamp();
        return true;
    }

    public void ReleaseRecoveryLock()
    {
        RecoveryInProgress = false;
        UpdateTimestamp();
    }

    public void RecordRecoveryAttempt(DateTime now)
    {
        RecoveryAttemptCount++;
        LastRecoveryAttemptAt = now;
        UpdateTimestamp(now);
    }

    public void RecordReminderSent(DateTime now)
    {
        LastReminderSentAt = now;
        ReminderCount++;
        UpdateTimestamp(now);
    }

    private string ComputeHmac(string rawToken, string serverSecret)
    {
        var key = Encoding.UTF8.GetBytes(serverSecret + Salt);
        using var hmac = new HMACSHA256(key);

        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = hmac.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
