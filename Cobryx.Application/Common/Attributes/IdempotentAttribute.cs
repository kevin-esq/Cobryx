using System;

namespace Cobryx.Application.Common.Attributes;

/// <summary>
/// Marks a controller action as idempotent, requiring an X-Idempotency-Key header.
/// Ensures the operation is executed at most once, caching the full response (headers + body).
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class IdempotentAttribute : Attribute
{
}
