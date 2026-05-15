namespace Cobryx.Domain.Accounting.Interfaces;

/// <summary>
/// Semantic marker interface for financial operations with high forensic risk.
/// Operations implementing this interface (e.g., Chargebacks, Manual Adjustments) 
/// will trigger immediate external anchoring regardless of the block sequence count.
/// </summary>
public interface IHighRiskOperation
{
}
