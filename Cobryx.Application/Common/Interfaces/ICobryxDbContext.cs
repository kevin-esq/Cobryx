using Microsoft.EntityFrameworkCore;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Entities.Invoicing;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Entities.Accounting;
using Cobryx.Application.Webhooks.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cobryx.Application.Common.Interfaces;

public interface ICobryxDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<PaymentLink> PaymentLinks { get; }
    DbSet<Customer> Customers { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<TenantSubscription> TenantSubscriptions { get; }
    DbSet<TenantGrowthMetrics> TenantGrowthMetrics { get; }
    DbSet<TenantMRRHistory> TenantMRRHistory { get; }
    DbSet<AdminActionAudit> AdminActionAudits { get; }

    // Lending
    DbSet<Domain.Entities.Lending.Loan> Loans { get; }
    DbSet<FinancialStatusAudit> FinancialStatusAudits { get; }

    // Payments & Accounting
    DbSet<Payment> Payments { get; }
    DbSet<PaymentMethod> PaymentMethods { get; }
    DbSet<LedgerAccount> LedgerAccounts { get; }
    DbSet<LedgerTransaction> LedgerTransactions { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }

    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<IDbContextTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted, CancellationToken ct = default);
}
