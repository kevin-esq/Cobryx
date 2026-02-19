using Cobryx.Application.Common.Configuration;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cobryx.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire recurring job that manages the lifecycle of tenant invitations:
/// - Mark Pending > TTL as Expired.
/// - Hard delete Accepted/Revoked/Expired > 30 days to prevent infinite table growth.
/// </summary>
public class InvitationCleanupJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly CobryxMetrics _metrics;
    private readonly AppOptions _appOptions;
    private readonly ILogger<InvitationCleanupJob> _logger;

    public InvitationCleanupJob(
        IUnitOfWork unitOfWork,
        IClock clock,
        CobryxMetrics metrics,
        IOptions<AppOptions> appOptions,
        ILogger<InvitationCleanupJob> logger)
    {
        _unitOfWork = unitOfWork;
        _clock = clock;
        _metrics = metrics;
        _appOptions = appOptions.Value;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        var db = (DbContext)_unitOfWork;
        var now = _clock.UtcNow;
        var batchSize = _appOptions.MaxInvitationCleanupBatchSize;

        // 1. Batch Expire: Pending > ExpiresAt
        var sqlExpire = @"
            SELECT * FROM ""TenantInvitations"" 
            WHERE ""Status"" = 0 AND ""ExpiresAt"" < @p0
            ORDER BY ""ExpiresAt"" ASC
            LIMIT @p1
            FOR UPDATE SKIP LOCKED";

        var toExpire = await db.Set<TenantInvitation>()
            .FromSqlRaw(sqlExpire, now, batchSize)
            .ToListAsync(ct);

        foreach (var inv in toExpire)
        {
            inv.MarkExpired();
            _metrics.CleanupInvitationsExpired.Add(1);
        }

        if (toExpire.Count > 0)
        {
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Batch-marked {Count} invitations as Expired (Distributed-safe)", toExpire.Count);
        }

        // 2. Batch Delete: Non-Pending > 30 days old
        var retentionThreshold = now.AddDays(-30);
        
        var sqlDelete = @"
            SELECT * FROM ""TenantInvitations"" 
            WHERE ""Status"" != 0 AND ""CreatedAt"" < @p0
            ORDER BY ""Id"" ASC
            LIMIT @p1
            FOR UPDATE SKIP LOCKED";

        var toCleanup = await db.Set<TenantInvitation>()
            .FromSqlRaw(sqlDelete, retentionThreshold, batchSize)
            .ToListAsync(ct);

        if (toCleanup.Count > 0)
        {
            db.Set<TenantInvitation>().RemoveRange(toCleanup);
            await _unitOfWork.SaveChangesAsync(ct);
            _metrics.CleanupInvitationsDeleted.Add(toCleanup.Count);
            _logger.LogInformation("Batch-deleted {Count} ancient records (Distributed-safe, Retention: 30 days)", toCleanup.Count);
        }
    }
}
