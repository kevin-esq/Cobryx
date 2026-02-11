namespace Cobryx.Domain.Interfaces;

public interface IInvoiceNumberService
{
    Task<string> GenerateNextNumberAsync(Guid tenantId);
}
