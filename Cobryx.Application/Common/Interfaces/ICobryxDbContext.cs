using Microsoft.EntityFrameworkCore;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Entities.Invoicing;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Entities.Accounting;
using Cobryx.Application.Webhooks.Entities;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cobryx.Application.Common.Interfaces;

public interface ICobryxDbContext
{
    DbSet<Tenant> Tenants { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<TenantInvitation> TenantInvitations { get; }
    DbSet<PaymentLink> PaymentLinks { get; }
    DbSet<Cobryx.Domain.Entities.Customer> Customers { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<TenantSubscription> TenantSubscriptions { get; }
    DbSet<TenantGrowthMetrics> TenantGrowthMetrics { get; }
    DbSet<TenantMRRHistory> TenantMRRHistory { get; }
    DbSet<AdminActionAudit> AdminActionAudits { get; }

    // Lending
    DbSet<Domain.Entities.Lending.Loan> Loans { get; }
    DbSet<LoanDelinquencyState> LoanDelinquencyStates { get; }
    DbSet<CollectionsPolicy> CollectionsPolicies { get; }
    DbSet<LoanCollectionsEvent> LoanCollectionsEvents { get; }
    DbSet<FinancialOutboxEvent> FinancialOutboxEvents { get; }
    DbSet<DeadLetterEvent> DeadLetterEvents { get; }
    DbSet<ProcessedEvent> ProcessedEvents { get; }
    DbSet<FinancialStatusAudit> FinancialStatusAudits { get; }
    DbSet<AccruedCharge> AccruedCharges { get; }
    DbSet<LoanPaymentAllocation> LoanPaymentAllocations { get; }

    // Payments & Accounting
    DbSet<Payment> Payments { get; }
    DbSet<PaymentMethod> PaymentMethods { get; }
    DbSet<LedgerAccount> LedgerAccounts { get; }
    DbSet<LedgerTransaction> LedgerTransactions { get; }
    DbSet<LedgerEntry> LedgerEntries { get; }
    DbSet<AccountBalanceSnapshot> AccountBalanceSnapshots { get; }
    DbSet<LedgerOutbox> LedgerOutboxes { get; }
    DbSet<BankMovement> BankMovements { get; }
    DbSet<JournalCheckpoint> JournalCheckpoints { get; }
    DbSet<ReconciliationAudit> ReconciliationAudits { get; }
    DbSet<ShadowBalance> ShadowBalances { get; }
    DbSet<EventShadowBalance> EventShadowBalances { get; }

    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<IDbContextTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted, CancellationToken ct = default);
    DatabaseFacade Database { get; }
}
