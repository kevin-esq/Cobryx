using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Services;

public static class TaxCalculator
{
    public static (decimal Subtotal, decimal TaxAmount, decimal Total) Calculate(decimal quantity, decimal unitPrice, decimal taxRate, bool isTaxInclusive)
    {
        decimal rawTotal = quantity * unitPrice;
        decimal subtotal;
        decimal tax;
        decimal total;

        if (isTaxInclusive)
        {
            subtotal = rawTotal / (1 + taxRate);
            tax = rawTotal - subtotal;
            total = rawTotal;
        }
        else
        {
            subtotal = rawTotal;
            tax = rawTotal * taxRate;
            total = rawTotal + tax;
        }

        return (
            Math.Round(subtotal, 2, MidpointRounding.AwayFromZero),
            Math.Round(tax, 2, MidpointRounding.AwayFromZero),
            Math.Round(total, 2, MidpointRounding.AwayFromZero)
        );
    }
}
