using System.Linq.Expressions;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Persistence;

public class CobryxDbContext : DbContext
{
    private readonly ITenantProvider _tenantProvider;

    public CobryxDbContext(DbContextOptions<CobryxDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Credit> Credits => Set<Credit>();
    public DbSet<Installment> Installments => Set<Installment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CobryxDbContext).Assembly);

        // Global Query Filters (Multi-tenancy & Soft Delete)
        var currentTenantId = _tenantProvider.GetTenantId();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Skip owned types as they are part of their owner
            if (entityType.IsOwned()) continue;
            // 1. Soft Delete Filter & Concurrency Token
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var filter = Expression.Lambda(
                    Expression.Equal(
                        Expression.Property(parameter, nameof(BaseEntity.IsDeleted)),
                        Expression.Constant(false)),
                    parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);

                // Configure RowVersion as concurrency token
                modelBuilder.Entity(entityType.ClrType)
                    .Property<byte[]>(nameof(BaseEntity.RowVersion))
                    .IsRowVersion();
            }

            // 2. Multi-tenancy Filter
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var filter = Expression.Lambda(
                    Expression.Equal(
                        Expression.Property(parameter, nameof(ITenantEntity.TenantId)),
                        Expression.Constant(currentTenantId ?? Guid.Empty)),
                    parameter);

                // Note: TenantId might need careful handling for null providers during migrations.
                // In a real pro app, we use a more robust way to combine filters.
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}
