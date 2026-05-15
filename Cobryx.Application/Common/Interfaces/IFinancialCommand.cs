namespace Cobryx.Application.Common.Interfaces;

/// <summary>
/// Marker interface for commands that execute sensitive financial operations.
/// Commands implementing this interface will NOT be automatically retried on concurrency conflicts
/// to prevent duplicate ledger entries or double-spend scenarios.
/// </summary>
public interface IFinancialCommand
{
}
