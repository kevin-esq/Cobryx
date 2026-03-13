using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;

using FluentAssertions;

namespace Cobryx.Domain.Tests.Lending;

public class LoanFinancialStatusTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Guid _agreementId = Guid.NewGuid();

    [Fact]
    public void UpdateFinancialRiskStatus_WithNoOverdueInstallments_ShouldBeCurrent()
    {
        // Arrange
        var today = new DateTime(2026, 1, 1);
        var loan = CreateLoan(1000);

        var installments = new List<Installment>
        {
            new(loan.Id, 1, today.AddDays(30), 500, 0),
            new(loan.Id, 2, today.AddDays(60), 500, 0)
        };
        loan.AddInstallments(installments);

        // Act
        loan.UpdateFinancialRiskStatus(today);

        // Assert
        loan.FinancialStatus.Should().Be(FinancialStatus.Current);
        loan.FinancialDaysPastDue.Should().Be(0);
        loan.ArrearsAmount.Should().Be(0);
    }

    [Theory]
    [InlineData(1, FinancialStatus.Current)]   // Grace period <= 3 days
    [InlineData(10, FinancialStatus.Late)]     // <= 30 days
    [InlineData(45, FinancialStatus.Delinquent)] // <= 90 days
    [InlineData(120, FinancialStatus.Default)]   // <= 180 days
    [InlineData(200, FinancialStatus.ChargedOff)] // > 180 days
    public void UpdateFinancialRiskStatus_WithOverdueInstallment_ShouldReflectCorrectStatus(int daysOverdue, FinancialStatus expectedStatus)
    {
        // Arrange
        var today = new DateTime(2026, 1, 1);
        var dueDate = today.AddDays(-daysOverdue);
        var loan = CreateLoan(1000);

        var installments = new List<Installment>
        {
            new Installment(loan.Id, 1, dueDate, 500, 0)
        };
        loan.AddInstallments(installments);

        // Act
        loan.UpdateFinancialRiskStatus(today);

        // Assert
        loan.FinancialStatus.Should().Be(expectedStatus);
        loan.FinancialDaysPastDue.Should().Be(daysOverdue);
        loan.ArrearsAmount.Should().Be(500);
    }

    [Fact]
    public void UpdateFinancialRiskStatus_WithPartialPayment_ShouldCalculateCorrectArrears()
    {
        // Arrange
        var today = new DateTime(2026, 1, 1);
        var dueDate = today.AddDays(-10);
        var loan = CreateLoan(1000);

        var installment = new Installment(loan.Id, 1, dueDate, 500, 50);
        installment.ApplyAllocation(100, PaymentApplicationType.Principal);
        installment.ApplyAllocation(10, PaymentApplicationType.Interest);

        loan.AddInstallments(new[] { installment });

        // Act
        loan.UpdateFinancialRiskStatus(today);

        // Assert
        loan.FinancialStatus.Should().Be(FinancialStatus.Late);
        loan.FinancialDaysPastDue.Should().Be(10);
        loan.ArrearsAmount.Should().Be(440); // (500-100) + (50-10) = 440
    }

    [Fact]
    public void MarkAsRecovered_ShouldOnlyWorkIfChargedOff()
    {
        // Arrange
        var loan = CreateLoan(1000);

        // Act & Assert
        loan.MarkAsRecovered();
        loan.FinancialStatus.Should().Be(FinancialStatus.Current);

        loan.MarkAsChargedOff();
        loan.MarkAsRecovered();
        loan.FinancialStatus.Should().Be(FinancialStatus.Recovered);
    }

    private Loan CreateLoan(decimal amount) => new(_tenantId, _customerId, _agreementId, "L-123", amount);
}
