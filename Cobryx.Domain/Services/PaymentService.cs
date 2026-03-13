using Cobryx.Domain.Accounting;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Services;

public class PaymentService
{
    public static void ApplyPaymentToInvoice(Payment payment, Invoice invoice, Money amount)
    {
        if (payment.TenantId != invoice.TenantId)
            throw new DomainException(DomainErrorCode.Invoicing.PaymentTenantMismatch);

        if (payment.CustomerId != invoice.CustomerId)
            throw new DomainException(DomainErrorCode.Invoicing.PaymentCustomerMismatch);

        invoice.ApplyPayment(payment.Id, amount);
        payment.AddAllocation(invoice.Id, amount);
    }
}
