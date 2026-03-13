using Cobryx.Application.Accounting.Services;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Accounting;
using Cobryx.Domain.Accounting.Enums;
using Cobryx.Domain.Lending;
using Cobryx.Domain.ValueObjects;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Moq;

namespace Cobryx.Application.Tests.Accounting.Services;

public class ReconciliationEngineTests
{
    private readonly Mock<IStripeService> _stripeMock;
    private readonly Mock<ILogger<ReconciliationEngine>> _loggerMock;
    private readonly Mock<ILogger<FinancialPostingEngine>> _postingLoggerMock;
    private readonly CobryxDbContext _dbContext;
    private readonly FinancialPostingEngine _postingEngine;
    private readonly CobryxMetrics _metrics;
    private readonly ReconciliationEngine _engine;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ReconciliationEngineTests()
    {
        _stripeMock = new Mock<IStripeService>();
        _loggerMock = new Mock<ILogger<ReconciliationEngine>>();
        _postingLoggerMock = new Mock<ILogger<FinancialPostingEngine>>();

        var options = new DbContextOptionsBuilder<CobryxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns(_tenantId);

        _dbContext = new CobryxDbContext(options, tenantProviderMock.Object);
        _postingEngine = new FinancialPostingEngine(_dbContext, _postingLoggerMock.Object);
        _metrics = new CobryxMetrics(); // Instantiate directly for tests
        _engine = new ReconciliationEngine(_dbContext, _stripeMock.Object, _postingEngine, _metrics, _loggerMock.Object);

        // Default: empty balance transactions to avoid breaking existing tests
        _stripeMock.Setup(s => s.ListBalanceTransactionsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task ReconcileAsync_ShouldDetectMissingPayment_WhenOutsideTolerance()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var intentId = "pi_missing_123";

        _stripeMock.Setup(s => s.GetBalanceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((1000m, 200m));

        _stripeMock.Setup(s => s.ListPaymentIntentsAsync(from, to, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new(intentId, 10000, "usd", "succeeded", DateTime.UtcNow.AddMinutes(-20), [])
            ]);

        // Act
        var audit = await _engine.ReconcileAsync(_tenantId, from, to);

        // Assert
        Assert.Equal(ReconciliationStatus.HardDrift, audit.Status);
        Assert.Equal(ReconciliationSeverity.Error, audit.Severity);
        Assert.Equal(1, audit.DetectedDriftsCount);
        Assert.Contains(intentId, audit.DriftDetailsJson);
        Assert.Contains("MissingPayment", audit.DriftDetailsJson);
    }

    [Fact]
    public async Task ReconcileAsync_ShouldDetectTimingLag_WhenInsideTolerance()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var intentId = "pi_lag_123";

        _stripeMock.Setup(s => s.GetBalanceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((1000m, 200m));

        _stripeMock.Setup(s => s.ListPaymentIntentsAsync(from, to, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                // Created 5 mins ago (within 15 min tolerance)
                new(intentId, 10000, "usd", "succeeded", DateTime.UtcNow.AddMinutes(-5), [])
            ]);

        // Act
        var audit = await _engine.ReconcileAsync(_tenantId, from, to);

        // Assert
        Assert.Equal(ReconciliationStatus.SoftDrift, audit.Status);
        Assert.Equal(ReconciliationSeverity.Info, audit.Severity);
        Assert.Equal(1, audit.DetectedDriftsCount);
        Assert.Contains("TimingLag", audit.DriftDetailsJson);
    }

    [Fact]
    public async Task ReconcileAsync_ShouldBeSynced_WhenLedgerMatchesStripe()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var intentId = "pi_synced_123";

        // Create Ledger Transaction to match (Standard constructor uses 4 args)
        var tx = new LedgerTransaction(_tenantId, "Sync Test", $"PAY-STRIPE-{intentId}");
        _dbContext.LedgerTransactions.Add(tx);
        await _dbContext.SaveChangesAsync();

        _stripeMock.Setup(s => s.GetBalanceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((1000m, 200m));

        _stripeMock.Setup(s => s.ListPaymentIntentsAsync(from, to, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new(intentId, 10000, "usd", "succeeded", DateTime.UtcNow.AddMinutes(-30), [])
            ]);

        // Act
        var audit = await _engine.ReconcileAsync(_tenantId, from, to);

        // Assert
        Assert.Equal(ReconciliationStatus.Synced, audit.Status);
        Assert.Equal(ReconciliationSeverity.Info, audit.Severity);
        Assert.Equal(0, audit.DetectedDriftsCount);
    }

    [Fact]
    public async Task ReconcileAsync_ShouldAutoRepairMissingLoanPayment()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var intentId = "pi_repair_123";
        var loanId = Guid.NewGuid();

        // 1. Setup a Loan and System Accounts in memory
        var customerId = Guid.NewGuid();
        var agreementId = Guid.NewGuid();
        var loan = new Loan(_tenantId, customerId, agreementId, "L-100", new Money(1000m, "USD"));
        // Force ID via reflection or just use the object
        typeof(Loan).GetProperty("Id")!.SetValue(loan, loanId);
        _dbContext.Loans.Add(loan);

        var cashAcc = new LedgerAccount(_tenantId, "1010", "Cash", LedgerAccountType.Asset, LedgerAccountRole.Available, "USD", true);
        var principalAcc = new LedgerAccount(_tenantId, "1210", "Principal", LedgerAccountType.Asset, LedgerAccountRole.Receivable, "USD", true);
        var interestAcc = new LedgerAccount(_tenantId, "4010", "Interest", LedgerAccountType.Revenue, LedgerAccountRole.None, "USD", true);
        var feeAcc = new LedgerAccount(_tenantId, "4020", "Fees", LedgerAccountType.Revenue, LedgerAccountRole.Fees, "USD", true);
        var lossAcc = new LedgerAccount(_tenantId, "5010", "Loss", LedgerAccountType.Expense, LedgerAccountRole.Loss, "USD", true);
        var recoveryAcc = new LedgerAccount(_tenantId, "4030", "Recovery", LedgerAccountType.Revenue, LedgerAccountRole.None, "USD", true);

        _dbContext.LedgerAccounts.AddRange(cashAcc, principalAcc, interestAcc, feeAcc, lossAcc, recoveryAcc);
        await _dbContext.SaveChangesAsync();

        _stripeMock.Setup(s => s.GetBalanceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((1000m, 200m));

        _stripeMock.Setup(s => s.ListPaymentIntentsAsync(from, to, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new(intentId, 50000, "usd", "succeeded", DateTime.UtcNow.AddMinutes(-30), new Dictionary<string, string>
                {
                    { "LoanId", loanId.ToString() }
                })
            ]);

        // Act
        // 1. First Pass: Detect but don't repair
        await _engine.ReconcileAsync(_tenantId, from, to);

        // 2. Second Pass: Confirm and Repair
        var audit = await _engine.ReconcileAsync(_tenantId, from, to);

        // Assert
        Assert.Equal(ReconciliationStatus.Repaired, audit.Status);
        Assert.Equal(0, audit.DetectedDriftsCount); // Drifts are cleared if repaired

        // Verify Ledger Transaction was created
        var txExists = await _dbContext.LedgerTransactions.AnyAsync(t => t.ReferenceId == $"PAY-STRIPE-{intentId}");
        Assert.True(txExists);
    }

    [Fact]
    public async Task ReconcileAsync_ShouldConfirmDrift_OnSecondRun()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var intentId = "pi_confirm_123";

        _stripeMock.Setup(s => s.ListPaymentIntentsAsync(from, to, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new(intentId, 10000, "usd", "succeeded", DateTime.UtcNow.AddMinutes(-30), [])
            ]);

        // 1. First Run: Detect but don't repair (not confirmed yet)
        await _engine.ReconcileAsync(_tenantId, from, to);

        // 2. Second Run: Should see it as Confirmed
        var audit = await _engine.ReconcileAsync(_tenantId, from, to);

        // Assert
        Assert.Contains("ConfirmedDrift", audit.DriftDetailsJson);
    }

    [Fact]
    public async Task ReconcileAsync_ShouldDetectSettlementAmountMismatch()
    {
        // Arrange
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var btId = "bt_settlement_123";
        var sourceId = "pi_source_123";

        // Create Ledger Transaction with mismatched amount
        var tx = new LedgerTransaction(_tenantId, "Mismatched Settlement", $"PAYOUT-STRIPE-{sourceId}");
        var cashAcc = new LedgerAccount(_tenantId, "1010", "Cash", LedgerAccountType.Asset, LedgerAccountRole.Available, "USD", true);
        _dbContext.LedgerAccounts.Add(cashAcc);
        tx.AddEntry(cashAcc.Id, 95.00m, 0); // Only 95 recorded
        _dbContext.LedgerTransactions.Add(tx);
        await _dbContext.SaveChangesAsync();

        _stripeMock.Setup(s => s.ListPaymentIntentsAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _stripeMock.Setup(s => s.ListBalanceTransactionsAsync(from, to, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                // BT shows 100 net
                new(btId, 10500, 500, 10000, "usd", "payout", "payout", "available", DateTime.UtcNow, DateTime.UtcNow, sourceId, [])
            ]);

        // Act
        var audit = await _engine.ReconcileAsync(_tenantId, from, to);

        // Assert
        Assert.Equal(ReconciliationStatus.HardDrift, audit.Status);
        Assert.Contains("AmountMismatch", audit.DriftDetailsJson);
        Assert.Contains("Settlement Amount Mismatch", audit.DriftDetailsJson);
    }
}
