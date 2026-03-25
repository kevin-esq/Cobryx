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

namespace Cobryx.Application.Tests.Accounting.Services;

public class ReconciliationEngineTests
{
    private readonly Mock<IStripeService> _stripeMock;
    private readonly CobryxDbContext _dbContext;
    private readonly ReconciliationEngine _engine;
    private readonly Guid _tenantId = Guid.NewGuid();

    public ReconciliationEngineTests()
    {
        _stripeMock = new Mock<IStripeService>();
        var loggerMock = new Mock<ILogger<ReconciliationEngine>>();
        var postingLoggerMock = new Mock<ILogger<FinancialPostingEngine>>();

        var options = new DbContextOptionsBuilder<CobryxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns(_tenantId);

        _dbContext = new CobryxDbContext(options, tenantProviderMock.Object);

        var postingEngine = new FinancialPostingEngine(_dbContext, postingLoggerMock.Object);
        var metrics = new CobryxMetrics();

        _engine = new ReconciliationEngine(_dbContext, _stripeMock.Object, postingEngine, metrics, loggerMock.Object);

        _stripeMock
            .Setup(s => s.GetBalanceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((1000m, 200m));

        _stripeMock
            .Setup(s => s.ListBalanceTransactionsAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task ReconcileAsync_ShouldDetectMissingPayment_WhenOutsideTolerance()
    {
        DateTime from = DateTime.UtcNow.AddHours(-1);
        DateTime to = DateTime.UtcNow;
        const string intentId = "pi_missing_123";

        _stripeMock
            .Setup(s => s.ListPaymentIntentsAsync(from, to,
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(intentId, 10000, "usd", "succeeded", DateTime.UtcNow.AddMinutes(-20), [])]);

        var audit = await _engine.ReconcileAsync(_tenantId, from, to);

        Assert.Equal(ReconciliationStatus.HardDrift, audit.Status);
        Assert.Equal(ReconciliationSeverity.Error, audit.Severity);
        Assert.Equal(1, audit.DetectedDriftsCount);
        Assert.Contains(intentId, audit.DriftDetailsJson);
        Assert.Contains("MissingPayment", audit.DriftDetailsJson);
    }

    [Fact]
    public async Task ReconcileAsync_ShouldDetectTimingLag_WhenInsideTolerance()
    {
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var intentId = "pi_lag_123";

        _stripeMock
            .Setup(s => s.ListPaymentIntentsAsync(from, to,
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(intentId, 10000, "usd", "succeeded", DateTime.UtcNow.AddMinutes(-5), [])]);

        var audit = await _engine.ReconcileAsync(_tenantId, from, to);

        Assert.Equal(ReconciliationStatus.SoftDrift, audit.Status);
        Assert.Equal(ReconciliationSeverity.Info, audit.Severity);
        Assert.Equal(1, audit.DetectedDriftsCount);
        Assert.Contains("TimingLag", audit.DriftDetailsJson);
    }

    [Fact]
    public async Task ReconcileAsync_ShouldBeSynced_WhenLedgerMatchesStripe()
    {
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var intentId = "pi_synced_123";

        _dbContext.LedgerTransactions.Add(new LedgerTransaction(_tenantId, "Sync Test", $"PAY-STRIPE-{intentId}"));
        await _dbContext.SaveChangesAsync();

        _stripeMock
            .Setup(s => s.ListPaymentIntentsAsync(from, to,
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(intentId, 10000, "usd", "succeeded", DateTime.UtcNow.AddMinutes(-30), [])]);

        var audit = await _engine.ReconcileAsync(_tenantId, from, to);

        Assert.Equal(ReconciliationStatus.Synced, audit.Status);
        Assert.Equal(ReconciliationSeverity.Info, audit.Severity);
        Assert.Equal(0, audit.DetectedDriftsCount);
    }

    [Fact]
    public async Task ReconcileAsync_ShouldAutoRepairMissingLoanPayment()
    {
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var intentId = "pi_repair_123";
        var loanId = Guid.NewGuid();

        var loan = new Loan(_tenantId, Guid.NewGuid(), Guid.NewGuid(), "L-100", new Money(1000m, "USD"));
        typeof(Loan).GetProperty("Id")!.SetValue(loan, loanId);
        _dbContext.Loans.Add(loan);

        _dbContext.LedgerAccounts.AddRange(
            new LedgerAccount(_tenantId, "1010", "Cash", LedgerAccountType.Asset, LedgerAccountRole.Available, "USD",
                true),
            new LedgerAccount(_tenantId, "1210", "Principal", LedgerAccountType.Asset, LedgerAccountRole.Receivable,
                "USD", true),
            new LedgerAccount(_tenantId, "4010", "Interest", LedgerAccountType.Revenue, LedgerAccountRole.None, "USD",
                true),
            new LedgerAccount(_tenantId, "4020", "Fees", LedgerAccountType.Revenue, LedgerAccountRole.Fees, "USD",
                true),
            new LedgerAccount(_tenantId, "5010", "Loss", LedgerAccountType.Expense, LedgerAccountRole.Loss, "USD",
                true),
            new LedgerAccount(_tenantId, "4030", "Recovery", LedgerAccountType.Revenue, LedgerAccountRole.None, "USD",
                true)
        );
        await _dbContext.SaveChangesAsync();

        _stripeMock
            .Setup(s => s.ListPaymentIntentsAsync(from, to,
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new(intentId, 50000, "usd", "succeeded", DateTime.UtcNow.AddMinutes(-30),
                    new Dictionary<string, string> { { "LoanId", loanId.ToString() } })
            ]);

        await _engine.ReconcileAsync(_tenantId, from, to);
        var audit = await _engine.ReconcileAsync(_tenantId, from, to);

        Assert.Equal(ReconciliationStatus.Repaired, audit.Status);
        Assert.Equal(0, audit.DetectedDriftsCount);
        Assert.True(await _dbContext.LedgerTransactions.AnyAsync(t => t.ReferenceId == $"PAY-STRIPE-{intentId}"));
    }

    [Fact]
    public async Task ReconcileAsync_ShouldConfirmDrift_OnSecondRun()
    {
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var intentId = "pi_confirm_123";

        _stripeMock
            .Setup(s => s.ListPaymentIntentsAsync(from, to,
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new(intentId, 10000, "usd", "succeeded", DateTime.UtcNow.AddMinutes(-30), [])]);

        await _engine.ReconcileAsync(_tenantId, from, to);
        await Task.Delay(100);

        var audit = await _engine.ReconcileAsync(_tenantId, from, to);

        Assert.Equal(ReconciliationStatus.ConfirmedDrift, audit.Status);
    }

    [Fact]
    public async Task ReconcileAsync_ShouldDetectSettlementAmountMismatch()
    {
        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow;
        var btId = "bt_settlement_123";
        var sourceId = "pi_source_123";

        var cashAcc = new LedgerAccount(_tenantId, "1010", "Cash", LedgerAccountType.Asset, LedgerAccountRole.Available,
            "USD", true);
        _dbContext.LedgerAccounts.Add(cashAcc);

        var tx = new LedgerTransaction(_tenantId, "Mismatched Settlement", $"PAYOUT-STRIPE-{sourceId}");
        tx.AddEntry(cashAcc.Id, 95.00m, 0);
        _dbContext.LedgerTransactions.Add(tx);

        await _dbContext.SaveChangesAsync();

        _stripeMock
            .Setup(s => s.ListPaymentIntentsAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _stripeMock
            .Setup(s => s.ListBalanceTransactionsAsync(from, to,
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new(btId, 10500, 500, 10000, "usd", "payout", "payout", "available",
                    DateTime.UtcNow, DateTime.UtcNow, sourceId, [])
            ]);

        var audit = await _engine.ReconcileAsync(_tenantId, from, to);

        Assert.Equal(ReconciliationStatus.HardDrift, audit.Status);
        Assert.Contains("AmountMismatch", audit.DriftDetailsJson);
        Assert.Contains("Settlement Amount Mismatch", audit.DriftDetailsJson);
    }
}