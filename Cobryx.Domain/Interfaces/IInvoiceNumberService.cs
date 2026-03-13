namespace Cobryx.Domain.Interfaces;

public interface IInvoiceNumberService
{
    public Task<string> GenerateNextNumberAsync(Guid tenantId);
}
