using System.Linq.Expressions;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Webhooks.Entities;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Analytics;
using Cobryx.Domain.Analytics.Risk;
using Cobryx.Domain.Collections;
using Cobryx.Domain.Decision;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Messaging;
using Cobryx.Domain.ML;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Shared;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cobryx.Infrastructure.Persistence
{
    public class CobryxDbContext(DbContextOptions<CobryxDbContext> options, ITenantProvider tenantProvider)
        : DbContext(options), ICobryxDbContext, IUnitOfWork
    {
        private static readonly SemaphoreSlim _sqliteLock = new(1, 1);

        public Guid CurrentTenantId => tenantProvider.GetTenantId() ?? Guid.Empty;

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var isSqlite = Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";
            if (isSqlite)
            {
                await _sqliteLock.WaitAsync(cancellationToken);
            }

            try
            {
                await HandleSqliteJournalSequencesAsync(cancellationToken);
                await NormalizeOrphanLoginSessionsAsync(cancellationToken);
                await NormalizeOrphanUserSecurityTokensAsync(cancellationToken);
                var result = await base.SaveChangesAsync(cancellationToken);
                return result;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries)
                {
                    var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                    Serilog.Log.Error(
                        "CONCURRENCY ERROR: Entity {EntityName}, State {State}, Id {Id}. Current Version: {CurrentVersion}, DB Version: {DbVersion}",
                        entry.Entity.GetType().Name,
                        entry.State,
                        entry.Property("Id").CurrentValue,
                        entry.Property("Version").CurrentValue,
                        databaseValues?.GetValue<long>("Version"));
                }

                throw;
            }
            catch (DbUpdateException ex)
            {
                var builder = new System.Text.StringBuilder();
                _ = builder.AppendLine($"--- DbUpdateException: {ex.Message} ---");
                if (ex.InnerException != null)
                {
                    _ = builder.AppendLine($"Inner: {ex.InnerException.Message}");
                }

                foreach (var entry in ChangeTracker.Entries())
                {
                    _ = builder.AppendLine($"Entity: {entry.Entity.GetType().Name} [State: {entry.State}]");
                    foreach (var prop in entry.Properties)
                    {
                        _ = builder.AppendLine($"  {prop.Metadata.Name}: {prop.CurrentValue}");
                    }
                }

                var logPath = "/tmp/ef_failure_diag.txt";
                File.WriteAllText(logPath, builder.ToString());
                throw;
            }
            finally
            {
                if (isSqlite)
                {
                    _ = _sqliteLock.Release();
                }
            }
        }

        /// <summary>
        /// EF occasionally tracks new <see cref="LoginSession"/> rows as <see cref="EntityState.Modified"/>
        /// (e.g. graph fix-up with refresh tokens), which generates UPDATEs against non-existent rows.
        /// Force INSERT semantics when the store has no row for the session id.
        /// </summary>
        private async Task NormalizeOrphanLoginSessionsAsync(CancellationToken cancellationToken)
        {
            foreach (var entry in ChangeTracker.Entries<LoginSession>())
            {
                if (entry.State != EntityState.Modified)
                {
                    continue;
                }

                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                if (databaseValues == null)
                {
                    entry.State = EntityState.Added;
                }
            }
        }

        /// <summary>
        /// EF occasionally tracks new <see cref="UserSecurityToken"/> rows as <see cref="EntityState.Modified"/>
        /// (graph fix-up with the parent user), which generates UPDATEs against non-existent rows.
        /// Force INSERT semantics when the store has no row for the token id.
        /// </summary>
        private async Task NormalizeOrphanUserSecurityTokensAsync(CancellationToken cancellationToken)
        {
            foreach (var entry in ChangeTracker.Entries<UserSecurityToken>())
            {
                if (entry.State != EntityState.Modified)
                {
                    continue;
                }

                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                if (databaseValues == null)
                {
                    entry.State = EntityState.Added;
                }
            }
        }

        private async Task HandleSqliteJournalSequencesAsync(CancellationToken ct)
        {
            var entries = ChangeTracker.Entries<LedgerEntry>()
                .Where(static e => e.State == EntityState.Added)
                .ToList();

            if (entries.Count == 0)
            {
                return;
            }

            var lastSequence = await LedgerEntries.IgnoreQueryFilters().AnyAsync(ct)
                ? await LedgerEntries.IgnoreQueryFilters().MaxAsync(static x => x.JournalSequenceId, ct)
                : 0;

            foreach (var entry in entries)
            {
                entry.Property(static x => x.JournalSequenceId).CurrentValue = ++lastSequence;
            }
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync(
            System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted,
            CancellationToken ct = default) => await Database.BeginTransactionAsync(isolationLevel, ct);


        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Payment> Payments => Set<Payment>();

        public DbSet<Domain.Payments.PaymentAllocation> PaymentAllocations =>
            Set<Domain.Payments.PaymentAllocation>();

        public DbSet<PaymentLink> PaymentLinks => Set<PaymentLink>();

        public DbSet<LoanBalanceSnapshot> LoanBalanceSnapshots => Set<LoanBalanceSnapshot>();
        public DbSet<CollectionCase> CollectionCases => Set<CollectionCase>();
        public DbSet<CollectionAction> CollectionActions => Set<CollectionAction>();
        public DbSet<CollectionPolicy> CollectionPolicies => Set<CollectionPolicy>();
        public DbSet<CollectionAgent> CollectionAgents => Set<CollectionAgent>();
        public DbSet<CollectionOutcome> CollectionOutcomes => Set<CollectionOutcome>();

        public DbSet<CustomerRiskProfile> CustomerRiskProfiles => Set<CustomerRiskProfile>();
        public DbSet<RiskEvent> RiskEvents => Set<RiskEvent>();
        public DbSet<DecisionSnapshot> DecisionSnapshots => Set<DecisionSnapshot>();
        public DbSet<ModelOutcome> ModelOutcomes => Set<ModelOutcome>();
        public DbSet<CustomerRiskSnapshot> CustomerRiskSnapshots => Set<CustomerRiskSnapshot>();
        public DbSet<ShadowPrediction> ShadowPredictions => Set<ShadowPrediction>();
        public DbSet<MlModel> MlModels => Set<MlModel>();
        public DbSet<DecisionOutcome> DecisionOutcomes => Set<DecisionOutcome>();
        public DbSet<QValue> QValues => Set<QValue>();
        public DbSet<Experience> Experiences => Set<Experience>();
        public DbSet<DecisionDistributionLog> DecisionDistributionLogs => Set<DecisionDistributionLog>();
        public DbSet<ReplaySnapshot> ReplaySnapshots => Set<ReplaySnapshot>();
        public DbSet<LatestLoanSnapshot> LatestLoanSnapshots => Set<LatestLoanSnapshot>();
        public DbSet<TenantPortfolioAggregate> TenantPortfolioAggregates => Set<TenantPortfolioAggregate>();
        public DbSet<PortfolioMetricsDaily> PortfolioMetricsDaily => Set<PortfolioMetricsDaily>();
        public DbSet<CashflowEvent> CashflowEvents => Set<CashflowEvent>();
        public DbSet<ProductionSnapshot> ProductionSnapshots => Set<ProductionSnapshot>();
        public DbSet<RegressionReport> RegressionReports => Set<RegressionReport>();
        public DbSet<ShadowDriftEvent> ShadowDriftEvents => Set<ShadowDriftEvent>();


        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Permission> Permissions => Set<Permission>();
        public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<SystemErrorLog> SystemErrorLogs => Set<SystemErrorLog>();
        public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
        public DbSet<LoginSession> LoginSessions => Set<LoginSession>();
        public DbSet<TaxConfiguration> TaxConfigurations => Set<TaxConfiguration>();
        public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
        public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
        public DbSet<LoanPaymentAllocation> LoanPaymentAllocations => Set<LoanPaymentAllocation>();
        public DbSet<LoanDelinquencyState> LoanDelinquencyStates => Set<LoanDelinquencyState>();
        public DbSet<CollectionsPolicy> CollectionsPolicies => Set<CollectionsPolicy>();
        public DbSet<LoanCollectionsEvent> LoanCollectionsEvents => Set<LoanCollectionsEvent>();
        public DbSet<DeadLetterEvent> DeadLetterEvents => Set<DeadLetterEvent>();
        public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
        public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
        public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
        public DbSet<UsageRecord> UsageRecords => Set<UsageRecord>();
        public DbSet<BillingAlert> BillingAlerts => Set<BillingAlert>();
        public DbSet<Coupon> Coupons => Set<Coupon>();
        public DbSet<CustomerSuggestion> CustomerSuggestions => Set<CustomerSuggestion>();
        public DbSet<ReleaseNote> ReleaseNotes => Set<ReleaseNote>();
        public DbSet<DocumentMetadata> Documents => Set<DocumentMetadata>();
        public DbSet<MfaDevice> MfaDevices => Set<MfaDevice>();
        public DbSet<RecoveryCode> RecoveryCodes => Set<RecoveryCode>();
        public DbSet<UserSecurityToken> SecurityTokens => Set<UserSecurityToken>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
        public DbSet<ProcessedStripeEvent> ProcessedStripeEvents => Set<ProcessedStripeEvent>();
        public DbSet<ReconciliationAudit> ReconciliationAudits => Set<ReconciliationAudit>();
        public DbSet<TenantInvitation> TenantInvitations => Set<TenantInvitation>();
        public DbSet<TenantGrowthMetrics> TenantGrowthMetrics => Set<TenantGrowthMetrics>();
        public DbSet<TenantMRRHistory> TenantMRRHistory => Set<TenantMRRHistory>();
        public DbSet<AdminActionAudit> AdminActionAudits => Set<AdminActionAudit>();
        public DbSet<LedgerAccount> LedgerAccounts => Set<LedgerAccount>();
        public DbSet<LedgerTransaction> LedgerTransactions => Set<LedgerTransaction>();
        public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
        public DbSet<AccountBalanceSnapshot> AccountBalanceSnapshots => Set<AccountBalanceSnapshot>();
        public DbSet<BankMovement> BankMovements => Set<BankMovement>();
        public DbSet<JournalCheckpoint> JournalCheckpoints => Set<JournalCheckpoint>();
        public DbSet<ShadowBalance> ShadowBalances => Set<ShadowBalance>();
        public DbSet<EventShadowBalance> EventShadowBalances => Set<EventShadowBalance>();
        public DbSet<FinancialStatusAudit> FinancialStatusAudits => Set<FinancialStatusAudit>();

        public DbSet<Domain.Idempotency.IdempotencyRecord> IdempotencyRecords => Set<Domain.Idempotency.IdempotencyRecord>();
        public DbSet<Domain.Webhooks.ProcessedWebhookEvent> ProcessedWebhookEvents => Set<Domain.Webhooks.ProcessedWebhookEvent>();

        public DbSet<BaseLendingInstrument> LendingInstruments => Set<BaseLendingInstrument>();
        public DbSet<Credit> Credits => Set<Credit>();
        public DbSet<Loan> Loans => Set<Loan>();
        public DbSet<LoanAgreement> LoanAgreements => Set<LoanAgreement>();
        public DbSet<Installment> Installments => Set<Installment>();
        public DbSet<CreditSale> CreditSales => Set<CreditSale>();
        public DbSet<InterestPolicy> InterestPolicies => Set<InterestPolicy>();
        public DbSet<LateFeePolicy> LateFeePolicies => Set<LateFeePolicy>();
        public DbSet<PaymentApplicationPolicy> PaymentApplicationPolicies => Set<PaymentApplicationPolicy>();
        public DbSet<AccruedCharge> AccruedCharges => Set<AccruedCharge>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            _ = modelBuilder.Entity<BaseLendingInstrument>().ToTable("LendingInstruments");
            _ = modelBuilder.Entity<Credit>().HasBaseType<BaseLendingInstrument>().ToTable("Credits");
            _ = modelBuilder.Entity<Loan>().HasBaseType<BaseLendingInstrument>().ToTable("Loans");

            _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(CobryxDbContext).Assembly);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (entityType.BaseType != null)
                {
                    continue;
                }

                var parameter = Expression.Parameter(entityType.ClrType, "e");
                Expression? filterExpr = null;

                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var isDeletedProperty = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                    var notDeletedExpr = Expression.Equal(isDeletedProperty, Expression.Constant(false));
                    filterExpr = notDeletedExpr;

                    if (entityType.IsOwned())
                    {
                        var versionProp = entityType.FindProperty(nameof(BaseEntity.Version));
                        if (versionProp != null)
                        {
                            versionProp.IsConcurrencyToken = true;
                        }
                    }
                    else
                    {
                        _ = modelBuilder.Entity(entityType.ClrType)
                            .Property<long>(nameof(BaseEntity.Version))
                            .IsConcurrencyToken();
                    }

                    _ = modelBuilder.Entity(entityType.ClrType).Ignore("RowVersion");
                    _ = modelBuilder.Entity(entityType.ClrType).Ignore("xmin");
                }

                if (filterExpr != null && !entityType.IsOwned())
                {
                    var lambda = Expression.Lambda(filterExpr, parameter);
                    _ = modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }

            _ = modelBuilder.Entity<SupportTicket>()
                .Property(static s => s.Status)
                .HasConversion<string>();

            _ = modelBuilder.Entity<SupportTicket>()
                .Property(static s => s.Priority)
                .HasConversion<string>();

            _ = modelBuilder.Entity<SupportTicket>()
                .Property(static s => s.Category)
                .HasConversion<string>();

            _ = modelBuilder.Entity<SupportTicket>()
                .HasIndex(static s => new { s.TenantId, s.Status });

            _ = modelBuilder.Entity<ReconciliationAudit>()
                .HasIndex(static a => new { a.TenantId, a.RunId });

            _ = modelBuilder.Entity<ReconciliationAudit>()
                .HasIndex(static a => new { a.TenantId, a.ToUtc });

            _ = modelBuilder.Entity<ProcessedStripeEvent>()
                .HasIndex(static e => new { e.StripeEventId, e.StripeAccountId })
                .IsUnique();

            _ = modelBuilder.Entity<LedgerEntry>()
                .HasIndex(static e => new { e.TenantId, e.AccountId, e.JournalSequenceId });

            _ = modelBuilder.Entity<LedgerEntry>()
                .HasIndex(static e => e.JournalSequenceId);

            _ = modelBuilder.Entity<AccountBalanceSnapshot>()
                .HasIndex(static s => new { s.TenantId, s.AccountId, s.JournalSequenceId })
                .IsDescending(false, false, true);

            _ = modelBuilder.Entity<LatestLoanSnapshot>().HasNoKey();
            _ = modelBuilder.Entity<TenantPortfolioAggregate>().HasNoKey();

            base.OnModelCreating(modelBuilder);
            _ = modelBuilder.Entity<ShadowBalance>(static b =>
            {
                _ = b.HasIndex(static x => new { x.TenantId, x.AccountId });
                _ = b.HasIndex(static x => x.LastSequence);
            });

            _ = modelBuilder.Entity<EventShadowBalance>(static b =>
            {
                _ = b.HasIndex(static x => new { x.TenantId, x.AccountId });
            });

            _ = modelBuilder.Entity<AccruedCharge>(static entity =>
            {
                _ = entity.HasKey(static e => e.Id);
                _ = entity.Property(static e => e.Amount).HasPrecision(18, 10);
                _ = entity.HasIndex(static e => new { e.LoanId, e.Type, e.AccrualDate }).IsUnique();
                _ = entity.HasOne(static e => e.Loan).WithMany(static l => l.AccruedCharges).HasForeignKey(static e => e.LoanId);
            });

            _ = modelBuilder.Entity<LoanPaymentAllocation>(static b =>
            {
                _ = b.HasIndex(static x => x.PaymentId).IsUnique();
            });

            _ = modelBuilder.Entity<LoanDelinquencyState>(static b =>
            {
                _ = b.HasIndex(static x => x.LoanId).IsUnique();
                _ = b.HasIndex(static x => x.Stage);
                _ = b.HasIndex(static x => x.DaysPastDue);
            });

            _ = modelBuilder.Entity<LoanCollectionsEvent>(static b =>
            {
                _ = b.HasIndex(static x => x.LoanId);
            });

            _ = modelBuilder.Entity<ProcessedEvent>(static b =>
            {
                _ = b.HasIndex(static x => new { x.EventId, x.ConsumerName }).IsUnique();
            });

            _ = modelBuilder.Entity<DeadLetterEvent>(static b =>
            {
                _ = b.HasIndex(static x => x.EventId).IsUnique();
            });
        }
    }
}
