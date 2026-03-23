using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;
using Cobryx.Infrastructure.Payments.Services;
using Cobryx.Infrastructure.Persistence;

using Concordia;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Moq;

namespace Cobryx.IntegrationTests.Payments;

public class PaymentOrchestrationServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly CobryxDbContext _dbContext;
    private readonly Mock<IStripeService> _stripeMock;
    private readonly Mock<ISender> _senderMock;
    private readonly Mock<ILogger<PaymentOrchestrationService>> _loggerMock;
    private readonly CobryxMetrics _metrics;
    private readonly Mock<ITenantProvider> _tenantProviderMock;
    private readonly PaymentOrchestrationService _service;

    public PaymentOrchestrationServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:;Foreign Keys=False");
        _connection.Open();

        var options = new DbContextOptionsBuilder<CobryxDbContext>()
            .UseSqlite(_connection)
            .Options;

        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock.Setup(t => t.GetTenantId()).Returns(Guid.NewGuid());

        _dbContext = new CobryxDbContext(options, _tenantProviderMock.Object);
        _dbContext.Database.EnsureCreated();

        _stripeMock = new Mock<IStripeService>();
        _senderMock = new Mock<ISender>();
        _loggerMock = new Mock<ILogger<PaymentOrchestrationService>>();
        _metrics = new CobryxMetrics();

        _service = new PaymentOrchestrationService(
            _dbContext,
            _stripeMock.Object,
            _senderMock.Object,
            _metrics,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandlePaymentFailureAsync_SoftDecline_ShouldScheduleRetry_AndUseIdempotency()
    {
        var tenantId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Test", "User", "+15550100", "test@email.com", null, null);
        customer.ToggleAutoPay(true);
        customer.SetDefaultPaymentMethod("pm_123");
        customer.SetStripeCustomerId("cus_123");
        _dbContext.Customers.Add(customer);

        var link = new PaymentLink(tenantId, customer.Id, new Money(100, "USD"), "token", DateTime.UtcNow.AddDays(1), "secret");
        _dbContext.PaymentLinks.Add(link);
        await _dbContext.SaveChangesAsync();

        var result = await _service.HandlePaymentFailureAsync(
            customer.Id,
            "insufficient_funds",
            100,
            "USD",
            "Initial failure",
            null,
            link.Id);

        Assert.True(result.IsSuccess);

        var expectedIdempotencyKey = $"recovery_{link.Id}_1";
        _stripeMock.Verify(s => s.ChargeSavedPaymentMethodAsync(
            "cus_123",
            "pm_123",
            100,
            "USD",
            It.Is<string>(d => d.Contains("Automated Recovery")),
            null, // stripeAccountId
            expectedIdempotencyKey,
            null,
            It.IsAny<CancellationToken>()), Times.Once);

        await _dbContext.Entry(link).ReloadAsync();
        Assert.Equal(1, link.RecoveryAttemptCount);
    }

    [Fact]
    public async Task HandlePaymentFailureAsync_HardDecline_ShouldGenerateManualLink()
    {
        var tenantId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Test", "User", "+15550100", "hard@email.com", null, null);
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        _senderMock.Setup(s => s.Send(It.IsAny<IRequest<Result<string>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success("token"));

        var result = await _service.HandlePaymentFailureAsync(
            customer.Id,
            "stolen_card",
            100,
            "USD",
            "Hard decline example");

        Assert.True(result.IsSuccess);
        _stripeMock.Verify(s => s.ChargeSavedPaymentMethodAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _senderMock.Verify(s => s.Send(It.IsAny<IRequest<Result<string>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandlePaymentFailureAsync_3DSRequired_ShouldEscalateToManualLink()
    {
        var tenantId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Test", "User", "+15550100", "3ds@email.com", null, null);
        customer.ToggleAutoPay(true);
        customer.SetDefaultPaymentMethod("pm_123");
        customer.SetStripeCustomerId("cus_123");
        _dbContext.Customers.Add(customer);
        await _dbContext.SaveChangesAsync();

        // Simulate Stripe authentication_required error
        _stripeMock.Setup(s => s.ChargeSavedPaymentMethodAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new global::Stripe.StripeException("authentication_required")
            {
                StripeError = new global::Stripe.StripeError { Type = "card_error", Code = "authentication_required" }
            });

        _senderMock.Setup(s => s.Send(It.IsAny<IRequest<Result<string>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success("3ds-token"));

        var result = await _service.HandlePaymentFailureAsync(
            customer.Id,
            "card_declined",
            100,
            "USD",
            "3DS Simulation");

        Assert.True(result.IsSuccess);
        _senderMock.Verify(s => s.Send(It.IsAny<IRequest<Result<string>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandlePaymentFailureAsync_ConcurrencyLock_ShouldPreventDoubleCharge()
    {
        var tenantId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Test", "User", "+15550100", "lock@email.com", null, null);
        _dbContext.Customers.Add(customer);

        var link = new PaymentLink(tenantId, customer.Id, new Money(100, "USD"), "token", DateTime.UtcNow.AddDays(1), "secret");
        link.TryAcquireRecoveryLock();
        _dbContext.PaymentLinks.Add(link);
        await _dbContext.SaveChangesAsync();

        var result = await _service.HandlePaymentFailureAsync(
            customer.Id,
            "insufficient_funds",
            100,
            "USD",
            "Double call simulation",
            null,
            link.Id);

        Assert.True(result.IsSuccess);
        _stripeMock.Verify(s => s.ChargeSavedPaymentMethodAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Already processing")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandlePaymentFailureAsync_LoanDisputed_ShouldAbortRecovery()
    {
        var tenantId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Test", "User", "+15550100", "dispute@email.com", null, null);
        customer.ToggleAutoPay(true);
        customer.SetDefaultPaymentMethod("pm_123");
        customer.SetStripeCustomerId("cus_123");
        _dbContext.Customers.Add(customer);

        var agreement = new LoanAgreement(tenantId, customer.Id, 100, Guid.NewGuid(), PaymentFrequency.Monthly, 1, DateTime.UtcNow, DateTime.UtcNow, LoanOrigin.CashLoan);
        _dbContext.LoanAgreements.Add(agreement);

        var loan = new Loan(tenantId, customer.Id, agreement.Id, "L-DISP", new Money(100, "USD"));
        loan.MarkAsDisputed();
        _dbContext.Loans.Add(loan);

        var link = new PaymentLink(tenantId, customer.Id, new Money(100, "USD"), "token", DateTime.UtcNow.AddDays(1), "secret", loan.Id);
        _dbContext.PaymentLinks.Add(link);
        await _dbContext.SaveChangesAsync();

        var result = await _service.HandlePaymentFailureAsync(
            customer.Id,
            "insufficient_funds",
            100,
            "USD",
            "Dispute test",
            null,
            link.Id);

        Assert.True(result.IsSuccess);
        _stripeMock.Verify(s => s.ChargeSavedPaymentMethodAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("dispute detected")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task HandlePaymentFailureAsync_LoanClosed_ShouldAbortRecovery()
    {
        var tenantId = Guid.NewGuid();
        var customer = new Customer(tenantId, "Test", "User", "+15550100", "closed@email.com", null, null);
        customer.ToggleAutoPay(true);
        customer.SetDefaultPaymentMethod("pm_123");
        customer.SetStripeCustomerId("cus_123");
        _dbContext.Customers.Add(customer);

        var agreement = new LoanAgreement(tenantId, customer.Id, 100, Guid.NewGuid(), PaymentFrequency.Monthly, 1, DateTime.UtcNow, DateTime.UtcNow, LoanOrigin.CashLoan);
        _dbContext.LoanAgreements.Add(agreement);

        var loan = new Loan(tenantId, customer.Id, agreement.Id, "L-CLOSED", new Money(100, "USD"));
        loan.MarkAsClosed();
        _dbContext.Loans.Add(loan);

        var link = new PaymentLink(tenantId, customer.Id, new Money(100, "USD"), "token", DateTime.UtcNow.AddDays(1), "secret", loan.Id);
        _dbContext.PaymentLinks.Add(link);
        await _dbContext.SaveChangesAsync();

        var result = await _service.HandlePaymentFailureAsync(
            customer.Id,
            "insufficient_funds",
            100,
            "USD",
            "Closed test",
            null,
            link.Id);

        Assert.True(result.IsSuccess);
        _stripeMock.Verify(s => s.ChargeSavedPaymentMethodAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Already settled or closed")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
        _metrics.Dispose();
        GC.SuppressFinalize(this);
    }
}
