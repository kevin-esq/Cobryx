namespace Cobryx.Domain.Interfaces;

public interface IVirusScanner
{
    Task<bool> IsSafeAsync(Stream file);
}
