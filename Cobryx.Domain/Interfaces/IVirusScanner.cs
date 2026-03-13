namespace Cobryx.Domain.Interfaces;

public interface IVirusScanner
{
    public Task<bool> IsSafeAsync(Stream file);
}
