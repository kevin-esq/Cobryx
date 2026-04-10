// Type alias for backward compatibility.
// Hangfire jobs serialized before the rename still reference this namespace.
// This shim allows those stale entries to resolve without cleaning the DB.
// Safe to remove once all old Hangfire records have expired.

namespace Cobryx.Infrastructure.BackgroundJobs;

/// <summary>
/// Backward-compatible alias for <see cref="Messaging.ProcessOutboxJob"/>.
/// Exists solely so Hangfire can deserialize jobs that were enqueued under the old namespace.
/// </summary>
public class ProcessOutboxJob(
    IServiceProvider serviceProvider,
    Microsoft.Extensions.Logging.ILogger<Messaging.ProcessOutboxJob> logger)
    : Messaging.ProcessOutboxJob(serviceProvider, logger);
