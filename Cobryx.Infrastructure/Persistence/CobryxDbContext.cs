using System.Linq.Expressions;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Persistence;

public class CobryxDbContext : DbContext, IUnitOfWork
{
    private readonly ITenantProvider _tenantProvider;

    public CobryxDbContext(DbContextOptions<CobryxDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public Guid CurrentTenantId => _tenantProvider.GetTenantId() ?? Guid.Empty;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Credit> Credits => Set<Credit>();
    public DbSet<Installment> Installments => Set<Installment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CobryxDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.IsOwned()) continue;

            var parameter = Expression.Parameter(entityType.ClrType, "e");
            Expression? filterExpr = null;

            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var isDeletedProperty = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var notDeletedExpr = Expression.Equal(isDeletedProperty, Expression.Constant(false));
                filterExpr = notDeletedExpr;

                modelBuilder.Entity(entityType.ClrType)
                    .Property<uint>(nameof(BaseEntity.Version))
                    .IsRowVersion();
            }

            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType) && entityType.ClrType != typeof(User))
            {
                var tenantIdProperty = Expression.Property(parameter, nameof(ITenantEntity.TenantId));

                var tenantFilterExpr = Expression.Equal(
                    tenantIdProperty,
                    Expression.Property(Expression.Constant(this), nameof(CurrentTenantId))
                );

                filterExpr = filterExpr == null
                    ? tenantFilterExpr
                    : Expression.AndAlso(filterExpr, tenantFilterExpr);
            }

            if (filterExpr != null)
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

        modelBuilder.Entity<SupportTicket>()
            .HasIndex(s => new { s.TenantId, s.Status });

        base.OnModelCreating(modelBuilder);
    }
}
