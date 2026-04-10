namespace Cobryx.Application.Common.Attributes;

/// <summary>
/// Marks a request as tenant-scoped. Requests with this attribute
/// MUST also implement IRequiresTenant. This is enforced by TenantPolicyTests.
///
/// Usage:
/// [TenantScoped]
/// public record GetPortfolioSummaryQuery : IRequest&lt;...&gt;, IRequiresTenant;
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class TenantScopedAttribute : Attribute { }
