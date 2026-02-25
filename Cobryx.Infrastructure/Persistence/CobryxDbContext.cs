using System.Linq.Expressions;
using Cobryx.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Application.Webhooks.Entities;
using Cobryx.Domain.Entities.Invoicing;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Entities.Accounting;
using Cobryx.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cobryx.Infrastructure.Persistence;

public class CobryxDbContext : DbContext, ICobryxDbContext, IUnitOfWork
{
    private readonly ITenantProvider _tenantProvider;
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = false
    };

    public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
    {
        public void Configure(EntityTypeBuilder<OutboxEvent> builder)
        {
            builder.ToTable("OutboxEvents");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.ProcessedOnUtc);
            builder.HasIndex(x => x.OccurredOnUtc);
        }
    }

    public CobryxDbContext(DbContextOptions<CobryxDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public Guid CurrentTenantId => _tenantProvider.GetTenantId() ?? Guid.Empty;

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted, CancellationToken ct = default)
    {
        return await Database.BeginTransactionAsync(isolationLevel, ct);
    }


    private void UpdateAuditFields()
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.IncrementVersion();
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity.Version == 0 && entry.Entity is not OutboxEvent)
                {
                    entry.State = EntityState.Added;
                    entry.Entity.IncrementVersion();
                }
                else
                {
                    entry.Entity.IncrementVersion();
                }
            }
        }
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Credit> Credits => Set<Credit>();
    public DbSet<Domain.Entities.Invoicing.Installment> Installments => Set<Domain.Entities.Invoicing.Installment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<PaymentLink> PaymentLinks => Set<PaymentLink>();
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
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<WebhookEvent> WebhookEvents => Set<WebhookEvent>();
    public DbSet<ProcessedStripeEvent> ProcessedStripeEvents => Set<ProcessedStripeEvent>();
    public DbSet<TenantInvitation> TenantInvitations => Set<TenantInvitation>();
    public DbSet<TenantGrowthMetrics> TenantGrowthMetrics => Set<TenantGrowthMetrics>();
    public DbSet<TenantMRRHistory> TenantMRRHistory => Set<TenantMRRHistory>();
    public DbSet<LedgerAccount> LedgerAccounts => Set<LedgerAccount>();
    public DbSet<LedgerTransaction> LedgerTransactions => Set<LedgerTransaction>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();
    public DbSet<FinancialStatusAudit> FinancialStatusAudits => Set<FinancialStatusAudit>();

    // Lending Domain
    public DbSet<Domain.Entities.Lending.Loan> Loans => Set<Domain.Entities.Lending.Loan>();
    public DbSet<Domain.Entities.Lending.LoanAgreement> LoanAgreements => Set<Domain.Entities.Lending.LoanAgreement>();
    public DbSet<Domain.Entities.Lending.Installment> LoanInstallments => Set<Domain.Entities.Lending.Installment>();
    public DbSet<Domain.Entities.Lending.CreditSale> CreditSales => Set<Domain.Entities.Lending.CreditSale>();
    public DbSet<Domain.Entities.Lending.InterestPolicy> InterestPolicies => Set<Domain.Entities.Lending.InterestPolicy>();
    public DbSet<Domain.Entities.Lending.LateFeePolicy> LateFeePolicies => Set<Domain.Entities.Lending.LateFeePolicy>();
    public DbSet<Domain.Entities.Lending.PaymentApplicationPolicy> PaymentApplicationPolicies => Set<Domain.Entities.Lending.PaymentApplicationPolicy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CobryxDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
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

        modelBuilder.Entity<Customer>()
            .HasIndex(c => new { c.TenantId, c.CreatedAt })
            .IsDescending(false, true);

        modelBuilder.Entity<Credit>()
            .HasIndex(c => new { c.TenantId, c.CreatedAt })
            .IsDescending(false, true);

        modelBuilder.Entity<Payment>()
            .HasIndex(p => new { p.TenantId, p.CreatedAt })
            .IsDescending(false, true);

        modelBuilder.Entity<Invoice>()
            .HasIndex(i => new { i.TenantId, i.Status });

        modelBuilder.Entity<Invoice>()
            .HasIndex(i => new { i.TenantId, i.CreatedAt });

        modelBuilder.Entity<User>()
            .HasIndex(u => new { u.TenantId, u.IsActive });

        modelBuilder.Entity<Loan>()
            .HasIndex(l => new { l.TenantId, l.IsDeleted });

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

        modelBuilder.Entity<ProcessedStripeEvent>()
            .HasIndex(e => new { e.StripeEventId, e.StripeAccountId })
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}
