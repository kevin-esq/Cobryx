namespace Cobryx.Application.Common.Interfaces;

/// <summary>
/// Marker interface for requests that require tenant context.
/// When implemented, the TenantValidationBehavior will automatically validate
/// that a tenant ID is present before the handler executes.
/// </summary>
public interface IRequiresTenant { }
