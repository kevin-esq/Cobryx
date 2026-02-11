using Cobryx.Api.Errors.Definitions;

namespace Cobryx.Api.Errors.Catalog;

public static class InvoicingErrors
{
    public static readonly ErrorDefinition InvoiceInvalidStatusForPayment = new(400, 5001);
    public static readonly ErrorDefinition InvoiceCurrencyMismatch = new(400, 5002);
    public static readonly ErrorDefinition PaymentNotProcessing = new(400, 5003);
    public static readonly ErrorDefinition InvoiceNotFound = new(404, 5004);
}
