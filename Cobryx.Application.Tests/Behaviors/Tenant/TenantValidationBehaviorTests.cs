using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;
using Cobryx.Infrastructure.Middleware;

using Concordia;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Tests.Behaviors.Tenant;

/// <summary>
/// Tests for TenantValidationBehavior to ensure it doesn't break:
/// - Webhooks (anonymous, no tenant)
/// - Background jobs (system context, no tenant)
/// - Public endpoints (no tenant required)
/// </summary>
public class TenantValidationBehaviorTests
{
    #region Opt-In Behavior Tests

    [Fact]
    public async Task TenantValidation_RequestWithoutMarker_PassesThrough()
    {
        // Arrange - Request that does NOT implement IRequiresTenant
        var request = new RequestWithoutTenantRequirement();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns((Guid?)null); // No tenant!

        var behavior = new PipelineBehaviors.TenantValidation<RequestWithoutTenantRequirement, Result<string>>(
            tenantProviderMock.Object,
            new Mock<ILogger<PipelineBehaviors.TenantValidation<RequestWithoutTenantRequirement, Result<string>>>>().Object);

        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success("OK"));
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled, "Handler should be called for requests without IRequiresTenant");
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TenantValidation_RequestWithMarker_AndValidTenant_PassesThrough()
    {
        // Arrange - Request that DOES implement IRequiresTenant
        var request = new RequestWithTenantRequirement();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns(Guid.NewGuid()); // Valid tenant

        var behavior = new PipelineBehaviors.TenantValidation<RequestWithTenantRequirement, Result<string>>(
            tenantProviderMock.Object,
            new Mock<ILogger<PipelineBehaviors.TenantValidation<RequestWithTenantRequirement, Result<string>>>>().Object);

        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success("OK"));
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled, "Handler should be called when tenant is valid");
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TenantValidation_RequestWithMarker_AndNoTenant_ReturnsFailure()
    {
        // Arrange - Request that DOES implement IRequiresTenant but NO tenant
        var request = new RequestWithTenantRequirement();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns((Guid?)null); // No tenant!

        var behavior = new PipelineBehaviors.TenantValidation<RequestWithTenantRequirement, Result<string>>(
            tenantProviderMock.Object,
            new Mock<ILogger<PipelineBehaviors.TenantValidation<RequestWithTenantRequirement, Result<string>>>>().Object);

        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success("OK"));
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.False(nextCalled, "Handler should NOT be called when tenant is missing");
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCode.Tenant.ContextMissing, result.Error);
    }

    [Fact]
    public async Task TenantValidation_RequestWithMarker_AndEmptyGuidTenant_ReturnsFailure()
    {
        // Arrange - Request with empty GUID tenant (invalid)
        var request = new RequestWithTenantRequirement();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns(Guid.Empty); // Empty GUID!

        var behavior = new PipelineBehaviors.TenantValidation<RequestWithTenantRequirement, Result<string>>(
            tenantProviderMock.Object,
            new Mock<ILogger<PipelineBehaviors.TenantValidation<RequestWithTenantRequirement, Result<string>>>>().Object);

        var nextCalled = false;
        RequestHandlerDelegate<Result<string>> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success("OK"));
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.False(nextCalled, "Handler should NOT be called when tenant is empty GUID");
        Assert.False(result.IsSuccess);
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public async Task WebhookCommand_WithoutTenantMarker_ProcessesSuccessfully()
    {
        // Arrange - Simulates a Stripe webhook (no tenant context)
        var request = new SimulatedWebhookCommand("evt_123", "payment_intent.succeeded");
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns((Guid?)null);

        var behavior = new PipelineBehaviors.TenantValidation<SimulatedWebhookCommand, Result>(
            tenantProviderMock.Object,
            new Mock<ILogger<PipelineBehaviors.TenantValidation<SimulatedWebhookCommand, Result>>>().Object);

        var nextCalled = false;
        RequestHandlerDelegate<Result> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success());
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled, "Webhook commands should process without tenant");
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task BackgroundJobCommand_WithoutTenantMarker_ProcessesSuccessfully()
    {
        // Arrange - Simulates a background job (system context)
        var request = new SimulatedBackgroundJobCommand();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns((Guid?)null);

        var behavior = new PipelineBehaviors.TenantValidation<SimulatedBackgroundJobCommand, Result>(
            tenantProviderMock.Object,
            new Mock<ILogger<PipelineBehaviors.TenantValidation<SimulatedBackgroundJobCommand, Result>>>().Object);

        var nextCalled = false;
        RequestHandlerDelegate<Result> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success());
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled, "Background jobs should process without tenant");
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task TenantScopedQuery_WithTenantMarker_RequiresTenant()
    {
        // Arrange - Simulates a tenant-scoped query
        var request = new SimulatedTenantScopedQuery();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns((Guid?)null);

        var behavior = new PipelineBehaviors.TenantValidation<SimulatedTenantScopedQuery, Result<object>>(
            tenantProviderMock.Object,
            new Mock<ILogger<PipelineBehaviors.TenantValidation<SimulatedTenantScopedQuery, Result<object>>>>().Object);

        var nextCalled = false;
        RequestHandlerDelegate<Result<object>> next = (ct) =>
        {
            nextCalled = true;
            return Task.FromResult(Result.Success<object>(new { }));
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.False(nextCalled, "Tenant-scoped queries should fail without tenant");
        Assert.False(result.IsSuccess);
    }

    #endregion

    #region Non-Generic Result Tests

    [Fact]
    public async Task TenantValidation_NonGenericResult_ReturnsCorrectFailure()
    {
        // Arrange
        var request = new RequestWithTenantRequirementNonGeneric();
        var tenantProviderMock = new Mock<ITenantProvider>();
        tenantProviderMock.Setup(x => x.GetTenantId()).Returns((Guid?)null);

        var behavior = new PipelineBehaviors.TenantValidation<RequestWithTenantRequirementNonGeneric, Result>(
            tenantProviderMock.Object,
            new Mock<ILogger<PipelineBehaviors.TenantValidation<RequestWithTenantRequirementNonGeneric, Result>>>().Object);

        RequestHandlerDelegate<Result> next = (ct) => Task.FromResult(Result.Success());

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCode.Tenant.ContextMissing, result.Error);
    }

    #endregion
}

#region Test Request Types

// Request WITHOUT IRequiresTenant - should always pass through
public record RequestWithoutTenantRequirement : IRequest<Result<string>>;

// Request WITH IRequiresTenant - should validate tenant
public record RequestWithTenantRequirement : IRequest<Result<string>>, IRequiresTenant;

// Non-generic result with tenant requirement
public record RequestWithTenantRequirementNonGeneric : IRequest<Result>, IRequiresTenant;

// Simulated webhook command (no tenant marker)
public record SimulatedWebhookCommand(string EventId, string EventType) : IRequest<Result>;

// Simulated background job (no tenant marker)
public record SimulatedBackgroundJobCommand : IRequest<Result>;

// Simulated tenant-scoped query (WITH tenant marker)
public record SimulatedTenantScopedQuery : IRequest<Result<object>>, IRequiresTenant;

#endregion
