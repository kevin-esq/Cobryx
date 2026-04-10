using System.Reflection;

using Cobryx.Api.Controllers.V1.Analytics;

namespace Cobryx.Integration.Tests.Architecture;

/// <summary>
/// Architecture tests for Controllers.
/// Controllers should be thin - no direct infrastructure access.
/// </summary>
public class ControllerArchitectureTests
{
    private static readonly Assembly ApiAssembly = typeof(PortfolioController).Assembly;

    // Controllers that are allowed to use infrastructure directly (with justification)
    private static readonly HashSet<string> ExemptControllers = new()
    {
        "HealthController", // Health checks need direct DB access for connection testing
    };

    /// <summary>
    /// Controllers should NOT directly use DbContext.
    /// They should delegate to MediatR handlers.
    /// </summary>
    [Fact]
    public void Controllers_ShouldNotDirectlyUse_DbContext()
    {
        var controllerTypes = ApiAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller"))
            .Where(t => !t.IsAbstract)
            .Where(t => !ExemptControllers.Contains(t.Name))
            .ToList();

        var violations = new List<string>();

        foreach (var controller in controllerTypes)
        {
            var constructorParams = controller.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType.Name)
                .ToList();

            if (constructorParams.Any(p => p.Contains("DbContext")))
            {
                violations.Add($"{controller.Name} directly uses DbContext");
            }
        }

        Assert.True(violations.Count == 0,
            $"Controllers with direct DbContext usage:\n{string.Join("\n", violations)}\n\n" +
            "Fix: Use MediatR to delegate to handlers instead, or add to ExemptControllers with justification.");
    }

    /// <summary>
    /// Controllers should NOT directly use Redis.
    /// </summary>
    [Fact]
    public void Controllers_ShouldNotDirectlyUse_Redis()
    {
        var controllerTypes = ApiAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller"))
            .Where(t => !t.IsAbstract)
            .ToList();

        var violations = new List<string>();

        foreach (var controller in controllerTypes)
        {
            var constructorParams = controller.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType.Name)
                .ToList();

            if (constructorParams.Any(p => p == "IConnectionMultiplexer" || p == "IDatabase"))
            {
                violations.Add($"{controller.Name} directly uses Redis");
            }
        }

        Assert.True(violations.Count == 0,
            $"Controllers with direct Redis usage:\n{string.Join("\n", violations)}\n\n" +
            "Fix: Use MediatR to delegate to handlers instead.");
    }

    /// <summary>
    /// Controllers should NOT directly use HttpClient.
    /// </summary>
    [Fact]
    public void Controllers_ShouldNotDirectlyUse_HttpClient()
    {
        var controllerTypes = ApiAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller"))
            .Where(t => !t.IsAbstract)
            .ToList();

        var violations = new List<string>();

        foreach (var controller in controllerTypes)
        {
            var constructorParams = controller.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType.Name)
                .ToList();

            if (constructorParams.Any(p => p == "HttpClient" || p == "IHttpClientFactory"))
            {
                violations.Add($"{controller.Name} directly uses HttpClient");
            }
        }

        Assert.True(violations.Count == 0,
            $"Controllers with direct HttpClient usage:\n{string.Join("\n", violations)}\n\n" +
            "Fix: Use MediatR to delegate to handlers instead.");
    }

    /// <summary>
    /// Controllers in enforced namespaces should use MediatR (ISender/IMediator).
    /// </summary>
    [Fact]
    public void NewControllers_ShouldUseMediatR()
    {
        // Controllers that have been refactored to use MediatR
        var enforcedControllers = new[]
        {
            "PortfolioController",
            "CollectionsController",
            // Add more as you refactor
        };

        var controllerTypes = ApiAssembly.GetTypes()
            .Where(t => enforcedControllers.Contains(t.Name))
            .ToList();

        var violations = new List<string>();

        foreach (var controller in controllerTypes)
        {
            var constructorParams = controller.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType.Name)
                .ToList();

            var usesMediatR = constructorParams.Any(p => p == "ISender" || p == "IMediator");

            if (!usesMediatR)
            {
                violations.Add($"{controller.Name} does not use MediatR (ISender/IMediator)");
            }
        }

        Assert.True(violations.Count == 0,
            $"Controllers not using MediatR:\n{string.Join("\n", violations)}\n\n" +
            "Fix: Inject ISender and delegate to handlers.");
    }

    /// <summary>
    /// Reports controller architecture health.
    /// </summary>
    [Fact]
    public void ReportControllerMetrics()
    {
        var controllerTypes = ApiAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller"))
            .Where(t => !t.IsAbstract)
            .ToList();

        var usingMediatR = controllerTypes
            .Where(c => c.GetConstructors()
                .SelectMany(ctor => ctor.GetParameters())
                .Any(p => p.ParameterType.Name == "ISender" || p.ParameterType.Name == "IMediator"))
            .Count();

        var usingDbContext = controllerTypes
            .Where(c => c.GetConstructors()
                .SelectMany(ctor => ctor.GetParameters())
                .Any(p => p.ParameterType.Name.Contains("DbContext")))
            .Count();

        var mediatRCoverage = controllerTypes.Count > 0 ? usingMediatR * 100 / controllerTypes.Count : 0;

        Assert.True(true,
            $"\n" +
            $"╔══════════════════════════════════════════╗\n" +
            $"║       CONTROLLER ARCHITECTURE            ║\n" +
            $"╠══════════════════════════════════════════╣\n" +
            $"║  Total Controllers:     {controllerTypes.Count,15} ║\n" +
            $"║  Using MediatR:         {usingMediatR,15} ║\n" +
            $"║  Using DbContext:       {usingDbContext,15} ║\n" +
            $"║  MediatR Coverage:      {mediatRCoverage,14}% ║\n" +
            $"╚══════════════════════════════════════════╝");
    }
}
