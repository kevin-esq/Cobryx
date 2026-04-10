namespace Cobryx.Api.Services;

/// <summary>
/// Centralized generator for resource URLs. 
/// Eliminates hardcoded strings in controllers and ensures Location headers are reliable.
/// </summary>
public interface IApiLinkGenerator
{
    public string? GetInvoiceUrl(Guid id, string version = "1");
    public string? GetPaymentUrl(Guid id, string version = "1");
    public string? GetProductUrl(Guid id, string version = "1");
    public string? GetTaxUrl(Guid id, string version = "1");
    public string? GetCreditUrl(Guid id, string version = "1");
    public string? GetLoanUrl(Guid id, string version = "1");
    public string? GetCustomerUrl(Guid id, string version = "1");
    public string? GetPaymentMethodUrl(Guid id, string version = "1");
    public string? GetUserUrl(Guid id, string version = "1");
    public string? GetDocumentUrl(Guid id, string version = "1");
}

public class ApiLinkGenerator(LinkGenerator linkGenerator, IHttpContextAccessor httpContextAccessor) : IApiLinkGenerator
{
    public string? GetInvoiceUrl(Guid id, string version = "1") =>
        BuildUrl("GetInvoice", new { version, id });

    public string? GetPaymentUrl(Guid id, string version = "1") =>
        BuildUrl("GetPayment", new { version, id });

    public string? GetProductUrl(Guid id, string version = "1") =>
        BuildUrl("GetProduct", new { version, id });

    public string? GetTaxUrl(Guid id, string version = "1") =>
        BuildUrl("GetTax", new { version, id });

    public string? GetCreditUrl(Guid id, string version = "1") =>
        BuildUrl("GetCredit", new { version, id });

    public string? GetLoanUrl(Guid id, string version = "1") =>
        BuildUrl("GetLoan", new { version, id });

    public string? GetCustomerUrl(Guid id, string version = "1") =>
        BuildUrl("GetCustomer", new { version, id });

    public string? GetPaymentMethodUrl(Guid id, string version = "1") =>
        BuildUrl("GetPaymentMethod", new { version, id });

    public string? GetUserUrl(Guid id, string version = "1") =>
        BuildUrl("GetUser", new { version, id });

    public string? GetDocumentUrl(Guid id, string version = "1") =>
        BuildUrl("GetDocument", new { version, id });

    private string? BuildUrl(string endpointName, object values)
    {
        var context = httpContextAccessor.HttpContext;
        if (context == null)
            return null;

        return linkGenerator.GetPathByRouteValues(context, endpointName, values);
    }
}
