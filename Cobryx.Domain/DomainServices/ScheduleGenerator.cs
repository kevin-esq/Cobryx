using Cobryx.Domain.Entities.Invoicing;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.DomainServices;

public class ScheduleGenerator : IScheduleGenerator
{
    public IEnumerable<Installment> GenerateSchedule(Entities.Credit credit)
    {
        var installments = new List<Installment>();
        decimal principal = credit.Principal.Amount;

        decimal periodicRate = GetPeriodicRate(credit.InterestRate / 100, credit.Frequency);
        int n = credit.InstallmentsCount;

        if (credit.InterestType == InterestType.Amortized)
        {
            if (periodicRate == 0)
            {
                decimal fixedPayment = Math.Round(principal / n, 2);
                decimal remainingPrincipal = principal;
                DateTime currentDate = credit.StartDate;

                for (int j = 1; j <= n; j++)
                {
                    currentDate = GetNextDueDate(currentDate, credit.Frequency);
                    decimal principalPart = j == n ? remainingPrincipal : fixedPayment;
                    remainingPrincipal -= principalPart;

                    installments.Add(new Installment(
                        credit.Id,
                        j,
                        currentDate,
                        new Money(principalPart, credit.Principal.Currency),
                        new Money(0, credit.Principal.Currency),
                        new Money(Math.Max(0, remainingPrincipal), credit.Principal.Currency)));
                }
            }
            else
            {
                decimal onePlusI_n = DecimalPower(1 + periodicRate, n);
                decimal numerator = principal * (periodicRate * onePlusI_n);
                decimal denominator = onePlusI_n - 1;

                decimal fixedPayment = Math.Round(numerator / denominator, 2);
                decimal remainingPrincipal = principal;
                DateTime currentDate = credit.StartDate;

                for (int j = 1; j <= n; j++)
                {
                    currentDate = GetNextDueDate(currentDate, credit.Frequency);
                    decimal interestPart = Math.Round(remainingPrincipal * periodicRate, 2);
                    decimal principalPart = j == n ? remainingPrincipal : fixedPayment - interestPart;
                    remainingPrincipal -= principalPart;

                    installments.Add(new Installment(
                        credit.Id,
                        j,
                        currentDate,
                        new Money(principalPart, credit.Principal.Currency),
                        new Money(interestPart, credit.Principal.Currency),
                        new Money(Math.Max(0, remainingPrincipal), credit.Principal.Currency)));
                }
            }
        }
        else
        {
            decimal totalInterest = credit.InterestType == InterestType.Simple
                ? Math.Round(principal * periodicRate * n, 2)
                : Math.Round(credit.InterestRate, 2);

            decimal totalDebt = principal + totalInterest;
            decimal installmentAmount = Math.Round(totalDebt / n, 2);
            decimal principalStep = Math.Round(principal / n, 2);
            decimal interestStep = Math.Round(totalInterest / n, 2);

            DateTime currentDate = credit.StartDate;
            decimal remainingBalance = totalDebt;

            for (int j = 1; j <= n; j++)
            {
                currentDate = GetNextDueDate(currentDate, credit.Frequency);

                if (j == n)
                {
                    installmentAmount = remainingBalance;
                }

                remainingBalance -= installmentAmount;

                installments.Add(new Installment(
                    credit.Id,
                    j,
                    currentDate,
                    new Money(principalStep, credit.Principal.Currency),
                    new Money(interestStep, credit.Principal.Currency),
                    new Money(Math.Max(0, remainingBalance), credit.Principal.Currency)));
            }
        }

        return installments;
    }

    private decimal GetPeriodicRate(decimal annualRate, PaymentFrequency frequency)
    {
        return frequency switch
        {
            PaymentFrequency.Daily => annualRate / 365,
            PaymentFrequency.Weekly => annualRate / 52,
            PaymentFrequency.BiWeekly => annualRate / 26,
            PaymentFrequency.Monthly => annualRate / 12,
            PaymentFrequency.SinglePayment => annualRate,
            _ => annualRate / 12
        };
    }

    private decimal DecimalPower(decimal x, int n)
    {
        decimal result = 1;
        for (int i = 0; i < n; i++)
        {
            result *= x;
            result = Math.Round(result, 10);
        }
        return result;
    }

    private DateTime GetNextDueDate(DateTime date, PaymentFrequency frequency)
    {
        return frequency switch
        {
            PaymentFrequency.Daily => date.AddDays(1),
            PaymentFrequency.Weekly => date.AddDays(7),
            PaymentFrequency.BiWeekly => date.AddDays(14),
            PaymentFrequency.Monthly => date.AddMonths(1),
            _ => date.AddMonths(1)
        };
    }
}
