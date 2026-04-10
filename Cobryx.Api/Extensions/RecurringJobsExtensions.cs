using Hangfire;

namespace Cobryx.Api.Extensions;

public static class RecurringJobsExtensions
{
    public static void RegisterCobryxRecurringJobs(this IServiceProvider _)
    {
        RegisterOutboxJobs();
        RegisterLedgerJobs();
        RegisterPaymentJobs();
        RegisterCollectionsJobs();
        RegisterAnalyticsJobs();
        RegisterMaintenanceJobs();
    }

    private static void RegisterOutboxJobs()
    {
        RecurringJob.AddOrUpdate<Infrastructure.Messaging.ProcessOutboxJob>(
            "process-outbox-events",
            job => job.RunAsync(CancellationToken.None),
            "*/10 * * * * *");

        RecurringJob.AddOrUpdate<Infrastructure.Messaging.LedgerOutboxWorker>(
            "ledger-cdc-outbox",
            job => job.ProcessEventsAsync(CancellationToken.None),
            "*/5 * * * * *");

        RecurringJob.AddOrUpdate<Infrastructure.Messaging.FinancialOutboxWorker>(
            "financial-events-outbox",
            job => job.ProcessEventsAsync(CancellationToken.None),
            "*/5 * * * * *");
    }

    private static void RegisterLedgerJobs()
    {
        RecurringJob.AddOrUpdate<Infrastructure.BackgroundJobs.Accounting.LedgerIntegrityJob>(
            "ledger-integrity-scan",
            job => job.RunAsync(CancellationToken.None),
            Cron.Hourly);

        RecurringJob.AddOrUpdate<Infrastructure.BackgroundJobs.Accounting.DriftDetectionWorker>(
            "ledger-drift-detection",
            job => job.ExecuteAsync(CancellationToken.None),
            Cron.Minutely);

        RecurringJob.AddOrUpdate<Infrastructure.BackgroundJobs.Accounting.LedgerInvariantsJob>(
            "ledger-invariants",
            job => job.RunAsync(CancellationToken.None),
            Cron.Hourly);
    }

    private static void RegisterPaymentJobs()
    {
        RecurringJob.AddOrUpdate<Infrastructure.BackgroundJobs.Payments.StripeReconciliationJob>(
            "stripe-reconciliation",
            job => job.RunAsync(CancellationToken.None),
            "*/5 * * * *");

        RecurringJob.AddOrUpdate<Infrastructure.BackgroundJobs.TenantConnectSyncJob>(
            "stripe-connect-sync",
            job => job.RunAsync(CancellationToken.None),
            Cron.Hourly);

        RecurringJob.AddOrUpdate<Infrastructure.BackgroundJobs.FinancialReconciliationJob>(
            "financial-reconciliation-recovery",
            job => job.RunAsync(CancellationToken.None),
            "*/30 * * * *");

        RecurringJob.AddOrUpdate<Infrastructure.BackgroundJobs.ExpirePaymentLinksJob>(
            "payment-link-expiration",
            job => job.RunAsync(CancellationToken.None),
            "*/15 * * * *");

        RecurringJob.AddOrUpdate<Infrastructure.BackgroundJobs.PaymentReminderJob>(
            "payment-collections-reminders",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily);
    }

    private static void RegisterCollectionsJobs()
    {
        RecurringJob.AddOrUpdate<Infrastructure.BackgroundJobs.Collections.CollectionsOrchestratorJob>(
            "collections-orchestrator",
            job => job.ProcessCollectionsAsync(),
            Cron.Hourly);

        RecurringJob.AddOrUpdate<Infrastructure.BackgroundJobs.Collections.CollectionsOptimizerJob>(
            "collections-ai-optimizer",
            job => job.RunHourlyOptimizationAsync(),
            Cron.Hourly);
    }

    private static void RegisterAnalyticsJobs()
    {
        RecurringJob.AddOrUpdate<Application.Analytics.Jobs.PortfolioMetricsJob>(
            "portfolio-metrics",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(0));

        RecurringJob.AddOrUpdate<Application.Analytics.Jobs.PortfolioCacheRefreshJob>(
            "portfolio-cache-refresh",
            job => job.RunAsync(CancellationToken.None),
            Cron.Daily(0, 30));

        RecurringJob.AddOrUpdate<Application.Risk.Jobs.EarlyWarningJob>(
            "risk-early-warning",
            job => job.RunAsync(CancellationToken.None),
            Cron.Hourly);

        RecurringJob.AddOrUpdate<Application.ML.Jobs.DatasetExporterJob>(
            "ml-dataset-export",
            job => job.RunAsync(),
            Cron.Daily);

        RecurringJob.AddOrUpdate<Application.ML.Jobs.RlTrainingJob>(
            "rl-training",
            job => job.RunAsync(),
            Cron.Hourly);
    }

    private static void RegisterMaintenanceJobs()
    {
        RecurringJob.AddOrUpdate<Infrastructure.BackgroundJobs.Lending.LoanAccrualWorker>(
            "loan-daily-accrual",
            job => job.ExecuteAsync(),
            "0 1 * * *");

        RecurringJob.AddOrUpdate<Infrastructure.BackgroundJobs.CleanupStaleDocumentsJob>(
            "cleanup-stale-documents",
            job => job.RunAsync(CancellationToken.None),
            "*/5 * * * *");

        RecurringJob.AddOrUpdate<Infrastructure.BackgroundJobs.InvitationCleanupJob>(
            "invitation-cleanup",
            job => job.RunAsync(CancellationToken.None),
            "*/10 * * * *");

        RecurringJob.AddOrUpdate<Infrastructure.BackgroundJobs.CheckSystemHealthJob>(
            "system-health-check",
            job => job.RunAsync(CancellationToken.None),
            "*/15 * * * *");
    }
}
