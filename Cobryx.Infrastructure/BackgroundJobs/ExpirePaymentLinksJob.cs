using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Payments.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire recurring job that scans for and expires PaymentLinks that have passed their TTL.
/// </summary>
public class ExpirePaymentLinksJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<ExpirePaymentLinksJob> _logger;

    public ExpirePaymentLinksJob(
        IUnitOfWork unitOfWork,
        IClock clock,
        ILogger<ExpirePaymentLinksJob> logger)
    {
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var db = (DbContext)_unitOfWork;
        var now = _clock.UtcNow;

        // 1. Find Active/Processing links that are past due
        var toExpire = await db.Set<PaymentLink>()
            .Where(l => (l.Status == PaymentLinkStatus.Active || l.Status == PaymentLinkStatus.Processing)
                        && l.ExpiresAt < now)
            .OrderBy(l => l.ExpiresAt)
            .Take(100) // Batch processing
            .ToListAsync(ct);

        if (toExpire.Count == 0)
            return;

        foreach (var link in toExpire)
        {
            link.Expire();
        }

        await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Expired {Count} payment links in background cleanup.", toExpire.Count);
    }
}
