using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Entities.Invoicing;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Common;

namespace Cobryx.Domain.Services;

public class PaymentService
{
    public void ApplyPaymentToInvoice(Payment payment, Invoice invoice, Money amount)
    {
        if (payment.TenantId != invoice.TenantId)
            throw new DomainException(DomainErrorCode.Invoicing.PaymentTenantMismatch);

        if (payment.CustomerId != invoice.CustomerId)
            throw new DomainException(DomainErrorCode.Invoicing.PaymentCustomerMismatch);

        invoice.ApplyPayment(payment.Id, amount);
        payment.AddAllocation(invoice.Id, amount);
    }
}
