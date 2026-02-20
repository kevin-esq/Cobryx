using Microsoft.EntityFrameworkCore;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Entities.Invoicing;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Application.Webhooks.Entities;

namespace Cobryx.Application.Common.Interfaces;

public interface ICobryxDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<Customer> Customers { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<TenantSubscription> TenantSubscriptions { get; }
    DbSet<TenantGrowthMetrics> TenantGrowthMetrics { get; }
    DbSet<TenantMRRHistory> TenantMRRHistory { get; }
    
    // Lending
    DbSet<Domain.Entities.Lending.Loan> Loans { get; }
    
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
