using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Payments.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs;

/// <summary>
/// The heartbeat of the Collections Engine. 
/// Automatically dispatches reminders based on strategic buckets and stop conditions.
/// </summary>
public class PaymentReminderJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IClock _clock;
    private readonly ILogger<PaymentReminderJob> _logger;

    public PaymentReminderJob(
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IClock clock,
        ILogger<PaymentReminderJob> logger)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _clock = clock;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var db = (DbContext)_unitOfWork;
        var now = _clock.UtcNow;

        var activeLinks = await db.Set<PaymentLink>()
            .Include(l => l.Customer)
            .Where(l => l.Status == PaymentLinkStatus.Active || l.Status == PaymentLinkStatus.Processing)
            .Where(l => l.ReminderCount < 4)
            .Where(l => l.LastReminderSentAt == null || l.LastReminderSentAt < now.AddHours(-24))
            .ToListAsync(ct);

        _logger.LogInformation("Collections Engine: Scanning {Count} active payment links.", activeLinks.Count);

        foreach (var link in activeLinks)
        {
            if (link.LoanId.HasValue)
            {
                var loan = await db.Set<Loan>().FindAsync(new object[] { link.LoanId.Value }, ct);

                if (loan == null ||
                    loan.Status == LoanStatus.Closed ||
                    loan.Status == LoanStatus.Cancelled ||
                    loan.Status == LoanStatus.WrittenOff ||
                    loan.Status == LoanStatus.Disputed)
                    continue;

                if (loan.LegalStatus == LegalStatus.Restructured ||
                    loan.LegalStatus == LegalStatus.InLegal ||
                    loan.LegalStatus == LegalStatus.PaymentPlanActive)
                    continue;

                if (loan.FinancialStatus == FinancialStatus.ChargedOff ||
                    loan.FinancialStatus == FinancialStatus.Recovered)
                    continue;
            }

            if (ShouldSendReminder(link, now))
            {
                await ProcessReminderAsync(link, ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static bool ShouldSendReminder(PaymentLink link, DateTime now)
    {
        var daysSinceCreation = (now - link.CreatedAt).TotalDays;
        var daysUntilExpiry = (link.ExpiresAt - now).TotalDays;

        if (daysUntilExpiry <= 3.1 && daysUntilExpiry > 2.0 && link.ReminderCount == 0)
            return true;

        if (daysSinceCreation >= 1.0 && daysSinceCreation < 2.0 && link.ReminderCount <= 1)
            return true;

        if (daysSinceCreation >= 7.0 && daysSinceCreation < 8.0 && link.ReminderCount <= 2)
            return true;

        if (daysSinceCreation >= 14.0 && daysSinceCreation < 15.0 && link.ReminderCount <= 3)
            return true;

        return false;
    }

    private async Task ProcessReminderAsync(PaymentLink link, CancellationToken ct)
    {
        try
        {
            var paymentUrl = $"https://pay.cobryx.com/l/{link.Salt}.{link.TokenHash.Substring(0, 8)}";

            var subject = $"Payment Reminder: Action Required for {link.Customer.FirstName}";
            var body = $@"Hello {link.Customer.FirstName},
            
This is a friendly reminder regarding your outstanding payment of {link.AmountSnapshot.Amount} {link.AmountSnapshot.Currency}.

You can securely complete your payment here: {paymentUrl}

If you have already paid, please ignore this message.

Thank you,
The Cobryx Team";

            await _emailService.SendEmailAsync(link.Customer.Email, subject, body, ct);

            link.RecordReminderSent();
            _logger.LogInformation("Collections Reminder sent for Link {LinkId}. Sequence: {Count}", link.Id, link.ReminderCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Collections Engine failed to send reminder for Link {LinkId}", link.Id);
        }
    }
}
