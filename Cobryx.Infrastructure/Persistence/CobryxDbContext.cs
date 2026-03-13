using System.Linq.Expressions;
using System.Text.Json;

using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Webhooks.Entities;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Messaging;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Shared;
using Cobryx.Domain.Analytics;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cobryx.Infrastructure.Persistence;

public class CobryxDbContext(DbContextOptions<CobryxDbContext> options, ITenantProvider tenantProvider) : DbContext(options), ICobryxDbContext, IUnitOfWork
{
    private readonly ITenantProvider _tenantProvider = tenantProvider;
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = false
    };

    private static readonly SemaphoreSlim _sqliteLock = new(1, 1);

    public Guid CurrentTenantId => _tenantProvider.GetTenantId() ?? Guid.Empty;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var isSqlite = Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";
        if (isSqlite) await _sqliteLock.WaitAsync(cancellationToken);

        try
        {
            await HandleSqliteJournalSequencesAsync(cancellationToken);
            var result = await base.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
            {
                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                Serilog.Log.Error("CONCURRENCY ERROR: Entity {EntityName}, State {State}, Id {Id}. Current Version: {CurrentVersion}, DB Version: {DbVersion}",
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
            builder.AppendLine($"--- DbUpdateException: {ex.Message} ---");
            if (ex.InnerException != null) builder.AppendLine($"Inner: {ex.InnerException.Message}");

            foreach (var entry in ChangeTracker.Entries())
            {
                builder.AppendLine($"Entity: {entry.Entity.GetType().Name} [State: {entry.State}]");
                foreach (var prop in entry.Properties)
                {
                    builder.AppendLine($"  {prop.Metadata.Name}: {prop.CurrentValue}");
                }
            }

            var logPath = "/tmp/ef_failure_diag.txt";
            System.IO.File.WriteAllText(logPath, builder.ToString());
            throw;
        }
        finally
        {
            if (isSqlite) _sqliteLock.Release();
        }
    }

    private async Task HandleSqliteJournalSequencesAsync(CancellationToken ct)
    {
        var entries = ChangeTracker.Entries<LedgerEntry>()
            .Where(e => e.State == EntityState.Added)
            .ToList();

        if (entries.Count == 0) return;

        var lastSequence = await LedgerEntries.IgnoreQueryFilters().AnyAsync(ct)
            ? await LedgerEntries.IgnoreQueryFilters().MaxAsync(x => x.JournalSequenceId, ct)
            : 0;

        foreach (var entry in entries)
        {
            entry.Property(x => x.JournalSequenceId).CurrentValue = ++lastSequence;
        }
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted, CancellationToken ct = default) => await Database.BeginTransactionAsync(isolationLevel, ct);



    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Cobryx.Domain.Lending.Customer> Customers => Set<Cobryx.Domain.Lending.Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Cobryx.Domain.Payments.PaymentAllocation> PaymentAllocations => Set<Cobryx.Domain.Payments.PaymentAllocation>();
    // Payment Links
    public DbSet<PaymentLink> PaymentLinks => Set<PaymentLink>();

    // Analytics
    public DbSet<LoanBalanceSnapshot> LoanBalanceSnapshots => Set<LoanBalanceSnapshot>();
    public DbSet<PortfolioMetricsDaily> PortfolioMetricsDaily => Set<PortfolioMetricsDaily>();
    public DbSet<CashflowEvent> CashflowEvents => Set<CashflowEvent>();

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

    // Lending Domain
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
        // Explicitly register the TPT hierarchy BEFORE applying assembly configurations
        // to prevent EF Core from discovering derived types arbitrarily due to reflection order.
        modelBuilder.Entity<BaseLendingInstrument>().ToTable("LendingInstruments");
        modelBuilder.Entity<Credit>().HasBaseType<BaseLendingInstrument>().ToTable("Credits");
        modelBuilder.Entity<Loan>().HasBaseType<BaseLendingInstrument>().ToTable("Loans");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CobryxDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.BaseType != null)
                continue;

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
                    modelBuilder.Entity(entityType.ClrType)
                        .Property<long>(nameof(BaseEntity.Version))
                        .IsConcurrencyToken();
                }

                // Ignore legacy shadow properties that cause SQLite issues
                modelBuilder.Entity(entityType.ClrType).Ignore("RowVersion");
                modelBuilder.Entity(entityType.ClrType).Ignore("xmin");
            }

            if (filterExpr != null && !entityType.IsOwned())
            {
                var lambda = Expression.Lambda(filterExpr, parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        modelBuilder.Entity<SupportTicket>()
            .Property(s => s.Status)
            .HasConversion<string>();

        modelBuilder.Entity<SupportTicket>()
            .Property(s => s.Priority)
            .HasConversion<string>();

        modelBuilder.Entity<SupportTicket>()
            .Property(s => s.Category)
            .HasConversion<string>();

        modelBuilder.Entity<SupportTicket>()
            .HasIndex(s => new { s.TenantId, s.Status });

        modelBuilder.Entity<ReconciliationAudit>()
            .HasIndex(a => new { a.TenantId, a.RunId });

        modelBuilder.Entity<ReconciliationAudit>()
            .HasIndex(a => new { a.TenantId, a.ToUtc });

        modelBuilder.Entity<ProcessedStripeEvent>()
            .HasIndex(e => new { e.StripeEventId, e.StripeAccountId })
            .IsUnique();

        modelBuilder.Entity<LedgerEntry>()
            .HasIndex(e => new { e.TenantId, e.AccountId, e.JournalSequenceId });

        modelBuilder.Entity<LedgerEntry>()
            .HasIndex(e => e.JournalSequenceId);

        modelBuilder.Entity<AccountBalanceSnapshot>()
            .HasIndex(s => new { s.TenantId, s.AccountId, s.JournalSequenceId })
            .IsDescending(false, false, true);

        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ShadowBalance>(b =>
        {
            b.HasIndex(x => new { x.TenantId, x.AccountId });
            b.HasIndex(x => x.LastSequence);
        });

        modelBuilder.Entity<EventShadowBalance>(b =>
        {
            b.HasIndex(x => new { x.TenantId, x.AccountId });
        });

        modelBuilder.Entity<AccruedCharge>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 10);
            entity.HasIndex(e => new { e.LoanId, e.Type, e.AccrualDate }).IsUnique();
            entity.HasOne(e => e.Loan).WithMany(l => l.AccruedCharges).HasForeignKey(e => e.LoanId);
        });

        modelBuilder.Entity<LoanPaymentAllocation>(b =>
        {
            b.HasIndex(x => x.PaymentId).IsUnique();
        });

        modelBuilder.Entity<LoanDelinquencyState>(b =>
        {
            b.HasIndex(x => x.LoanId).IsUnique();
            b.HasIndex(x => x.Stage);
            b.HasIndex(x => x.DaysPastDue);
        });

        modelBuilder.Entity<LoanCollectionsEvent>(b =>
        {
            b.HasIndex(x => x.LoanId);
        });

        modelBuilder.Entity<ProcessedEvent>(b =>
        {
            b.HasIndex(x => new { x.EventId, x.ConsumerName }).IsUnique();
        });

        modelBuilder.Entity<DeadLetterEvent>(b =>
        {
            b.HasIndex(x => x.EventId).IsUnique();
        });
    }
}
