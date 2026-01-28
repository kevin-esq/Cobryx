using Cobryx.Domain.Entities;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Services;

public class PaymentService
{
    public void ApplyPaymentToInvoice(Payment payment, Invoice invoice, Money amount)
    {
        if (payment.TenantId != invoice.TenantId)
            throw new InvalidOperationException("Tenant mismatch.");

        if (payment.CustomerId != invoice.CustomerId)
            throw new InvalidOperationException("Customer mismatch.");

        invoice.ApplyPayment(amount);
        payment.AddAllocation(invoice.Id, amount);
    }
}
