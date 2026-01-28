namespace Cobryx.Application.Common.Interfaces;

public interface IInvoiceNumberService
{
    Task<string> GenerateNextNumberAsync(Guid tenantId);
}
