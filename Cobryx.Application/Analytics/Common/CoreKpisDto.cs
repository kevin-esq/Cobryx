using System.Diagnostics.CodeAnalysis;

namespace Cobryx.Application.Analytics.Common;

/// <summary>
/// The 4 most critical business KPIs derived from the portfolio summary cache.
/// </summary>
/// <remarks>
/// Properties are consumed via JSON serialization by the ASP.NET response pipeline.
/// </remarks>
[SuppressMessage("ReSharper", "UnusedMember.Global",
    Justification = "Serialized to JSON by ASP.NET — not accessed directly in C# code.")]
public record CoreKpisDto(
    decimal TotalOutstanding,
    decimal NplRatio,
    decimal RevenueMtd,
    decimal CollectionEfficiency);
