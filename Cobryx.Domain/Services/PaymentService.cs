using Cobryx.Domain.Entities;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Common;

namespace Cobryx.Domain.Services;

public class PaymentService
{
    public void ApplyPaymentToInvoice(Payment payment, Invoice invoice, Money amount)
    {
        if (payment.TenantId != invoice.TenantId)
            throw new DomainException("DOMAIN.PAYMENT.TENANT_MISMATCH");

        if (payment.CustomerId != invoice.CustomerId)
            throw new DomainException("DOMAIN.PAYMENT.CUSTOMER_MISMATCH");

        invoice.ApplyPayment(amount);
        payment.AddAllocation(invoice.Id, amount);
    }
}
