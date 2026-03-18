using Cobryx.Domain.Accounting;
using Cobryx.Domain.Analytics.Risk;
using Cobryx.Domain.Collections;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Messaging;
using Cobryx.Domain.Payments;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cobryx.Application.Common.Interfaces;

public interface ICobryxDbContext
{
    public DbSet<Tenant> Tenants { get; }
    public DbSet<User> Users { get; }
    public DbSet<Role> Roles { get; }
    public DbSet<TenantInvitation> TenantInvitations { get; }
    public DbSet<PaymentLink> PaymentLinks { get; }
    public DbSet<Cobryx.Domain.Lending.Customer> Customers { get; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    public DbSet<TenantSubscription> TenantSubscriptions { get; }
    public DbSet<TenantGrowthMetrics> TenantGrowthMetrics { get; }
    public DbSet<TenantMRRHistory> TenantMRRHistory { get; }
    public DbSet<AdminActionAudit> AdminActionAudits { get; }

    // Analytics
    public DbSet<Domain.Analytics.LatestLoanSnapshot> LatestLoanSnapshots { get; }
    public DbSet<Cobryx.Domain.Analytics.TenantPortfolioAggregate> TenantPortfolioAggregates { get; }
    public DbSet<Domain.Analytics.PortfolioMetricsDaily> PortfolioMetricsDaily { get; }

    // Lending
    public DbSet<Loan> Loans { get; }
    public DbSet<Installment> Installments { get; }
    public DbSet<LoanDelinquencyState> LoanDelinquencyStates { get; }
    public DbSet<CollectionsPolicy> CollectionsPolicies { get; }
    public DbSet<LoanCollectionsEvent> LoanCollectionsEvents { get; }
    public DbSet<OutboxMessage> OutboxMessages { get; }
    public DbSet<DeadLetterEvent> DeadLetterEvents { get; }
    public DbSet<ProcessedEvent> ProcessedEvents { get; }
    public DbSet<FinancialStatusAudit> FinancialStatusAudits { get; }
    public DbSet<AccruedCharge> AccruedCharges { get; }
    public DbSet<LoanPaymentAllocation> LoanPaymentAllocations { get; }

    // Payments & Accounting
    public DbSet<Payment> Payments { get; }
    public DbSet<PaymentMethod> PaymentMethods { get; }
    public DbSet<LedgerAccount> LedgerAccounts { get; }
    public DbSet<LedgerTransaction> LedgerTransactions { get; }
    public DbSet<LedgerEntry> LedgerEntries { get; }
    public DbSet<AccountBalanceSnapshot> AccountBalanceSnapshots { get; }
    public DbSet<BankMovement> BankMovements { get; }
    public DbSet<JournalCheckpoint> JournalCheckpoints { get; }
    public DbSet<ReconciliationAudit> ReconciliationAudits { get; }
    public DbSet<ShadowBalance> ShadowBalances { get; }
    public DbSet<EventShadowBalance> EventShadowBalances { get; }

    public DbSet<CollectionCase> CollectionCases { get; }
    public DbSet<CollectionAction> CollectionActions { get; }
    public DbSet<CollectionPolicy> CollectionPolicies { get; }
    public DbSet<CollectionAgent> CollectionAgents { get; }
    public DbSet<CollectionOutcome> CollectionOutcomes { get; }

    // Risk
    public DbSet<CustomerRiskProfile> CustomerRiskProfiles { get; }
    public DbSet<RiskEvent> RiskEvents { get; }
    public DbSet<Cobryx.Domain.Decision.DecisionSnapshot> DecisionSnapshots { get; }
    public DbSet<Cobryx.Domain.ML.ModelOutcome> ModelOutcomes { get; }
    public DbSet<CustomerRiskSnapshot> CustomerRiskSnapshots { get; }

    public DbSet<TEntity> Set<TEntity>() where TEntity : class;
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    public Task<IDbContextTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted, CancellationToken ct = default);
    public DatabaseFacade Database { get; }
}
