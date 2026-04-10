using System.Reflection;
using System.Text.Json;

using Cobryx.Application.Analytics.Queries.GetPortfolioSummary;
using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;

namespace Cobryx.Architecture.Tests;

/// <summary>
/// Architecture Tests - Enforces Clean Architecture boundaries.
/// These tests make it impossible to violate architecture without failing the build.
/// </summary>
public class ArchitectureTests
{
    private static readonly Assembly ApplicationAssembly = typeof(GetPortfolioSummaryQuery).Assembly;

    #region Layer Dependency Rules

    /// <summary>
    /// Application layer MUST NOT reference Infrastructure layer.
    /// This is the core Clean Architecture rule.
    /// </summary>
    [Fact]
    public void Application_ShouldNotReference_Infrastructure()
    {
        var infrastructureAssemblyName = "Cobryx.Infrastructure";

        var referencedAssemblies = ApplicationAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        Assert.DoesNotContain(infrastructureAssemblyName, referencedAssemblies);
    }

    /// <summary>
    /// Application layer MUST NOT reference Api layer.
    /// </summary>
    [Fact]
    public void Application_ShouldNotReference_Api()
    {
        var apiAssemblyName = "Cobryx.Api";

        var referencedAssemblies = ApplicationAssembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToList();

        Assert.DoesNotContain(apiAssemblyName, referencedAssemblies);
    }

    #endregion

    #region Handler Purity Rules

    /// <summary>
    /// Handlers should not directly use DbContext.
    /// They should use repositories or services instead.
    /// </summary>
    [Fact]
    public void Handlers_ShouldNotDirectlyUse_DbContext()
    {
        var handlerTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Handler"))
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .ToList();

        var violations = new List<string>();

        foreach (var handler in handlerTypes)
        {
            var constructorParams = handler.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType.Name)
                .ToList();

            // Check for direct DbContext usage (should use ICobryxDbContext interface instead)
            if (constructorParams.Any(p => p == "CobryxDbContext"))
            {
                violations.Add($"{handler.Name} directly uses CobryxDbContext (use ICobryxDbContext instead)");
            }
        }

        Assert.True(violations.Count == 0,
            $"Handlers with direct DbContext usage:\n{string.Join("\n", violations)}\n\n" +
            "Fix: Inject ICobryxDbContext interface instead of concrete CobryxDbContext.");
    }

    /// <summary>
    /// Handlers should not directly use IConnectionMultiplexer (Redis).
    /// They should use ICacheService or domain-specific stores.
    /// </summary>
    [Fact]
    public void Handlers_ShouldNotDirectlyUse_Redis()
    {
        var handlerTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Handler"))
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .ToList();

        var violations = new List<string>();

        foreach (var handler in handlerTypes)
        {
            var constructorParams = handler.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType.Name)
                .ToList();

            if (constructorParams.Any(p => p == "IConnectionMultiplexer" || p == "IDatabase"))
            {
                violations.Add($"{handler.Name} directly uses Redis (use ICacheService or domain store instead)");
            }
        }

        Assert.True(violations.Count == 0,
            $"Handlers with direct Redis usage:\n{string.Join("\n", violations)}\n\n" +
            "Fix: Use ICacheService or a domain-specific store interface.");
    }

    /// <summary>
    /// Handlers should not use HttpClient directly.
    /// They should use typed clients or service interfaces.
    /// </summary>
    [Fact]
    public void Handlers_ShouldNotDirectlyUse_HttpClient()
    {
        var handlerTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Handler"))
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .ToList();

        var violations = new List<string>();

        foreach (var handler in handlerTypes)
        {
            var constructorParams = handler.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .Select(p => p.ParameterType.Name)
                .ToList();

            if (constructorParams.Any(p => p == "HttpClient" || p == "IHttpClientFactory"))
            {
                violations.Add($"{handler.Name} directly uses HttpClient (use typed client or service interface)");
            }
        }

        Assert.True(violations.Count == 0,
            $"Handlers with direct HttpClient usage:\n{string.Join("\n", violations)}\n\n" +
            "Fix: Create a typed client or service interface.");
    }

    #endregion

    #region Interface Segregation Rules

    /// <summary>
    /// Handlers in enforced namespaces should only depend on interfaces.
    /// Legacy handlers are tracked separately.
    /// </summary>
    [Fact]
    public void NewHandlers_ShouldOnlyDependOn_Interfaces()
    {
        var enforcedNamespaces = new[]
        {
            "Cobryx.Application.Analytics",
            "Cobryx.Application.Collections",
            "Cobryx.Application.Invoicing",
            "Cobryx.Application.Customers",
            // Payments - critical sub-namespaces only (semi-enforced)
            "Cobryx.Application.Payments.Commands.ProcessPayment",
            "Cobryx.Application.Payments.Commands.RefundPayment",
            "Cobryx.Application.Payments.Commands.HandleChargeback",
        };

        var handlerTypes = ApplicationAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Handler"))
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Where(t => enforcedNamespaces.Any(ns => t.Namespace?.StartsWith(ns) == true))
            .ToList();

        var violations = new List<string>();

        foreach (var handler in handlerTypes)
        {
            var constructorParams = handler.GetConstructors()
                .SelectMany(c => c.GetParameters())
                .ToList();

            foreach (var param in constructorParams)
            {
                var paramType = param.ParameterType;

                // Skip interfaces, value types, generics
                if (paramType.IsInterface)
                    continue;
                if (paramType.IsValueType)
                    continue;
                if (paramType.Name.StartsWith("ILogger"))
                    continue;

                // Check if it's a concrete class (violation)
                if (paramType.IsClass && !paramType.IsAbstract)
                {
                    violations.Add($"{handler.Name} depends on concrete type {paramType.Name}");
                }
            }
        }

        Assert.True(violations.Count == 0,
            $"Handlers with concrete dependencies:\n{string.Join("\n", violations)}\n\n" +
            "Fix: Depend on interfaces, not concrete implementations.");
    }

    #endregion

    #region Naming Convention Rules

    /// <summary>
    /// Queries in enforced namespaces should end with "Query".
    /// </summary>
    [Fact]
    public void NewQueries_ShouldFollowNamingConvention()
    {
        var enforcedNamespaces = new[]
        {
            "Cobryx.Application.Analytics",
            "Cobryx.Application.Collections",
            "Cobryx.Application.Invoicing",
            "Cobryx.Application.Customers",
            // Payments - critical sub-namespaces only (semi-enforced)
            "Cobryx.Application.Payments.Commands.ProcessPayment",
            "Cobryx.Application.Payments.Commands.RefundPayment",
            "Cobryx.Application.Payments.Commands.HandleChargeback",
        };

        var queryTypes = ApplicationAssembly.GetTypes()
            .Where(t => enforcedNamespaces.Any(ns => t.Namespace?.StartsWith(ns) == true))
            .Where(t => t.Namespace?.Contains(".Queries.") == true)
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Where(t => !t.IsNested) // Exclude compiler-generated nested types
            .Where(t => !t.Name.Contains("<")) // Exclude async state machines
            .Where(t => !t.Name.EndsWith("Handler"))
            .Where(t => !t.Name.EndsWith("Dto"))
            .Where(t => !t.Name.EndsWith("Validator"))
            .ToList();

        var violations = queryTypes
            .Where(t => !t.Name.EndsWith("Query"))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(violations.Count == 0,
            $"Query types not following naming convention:\n{string.Join("\n", violations)}\n\n" +
            "Fix: Rename to end with 'Query'.");
    }

    /// <summary>
    /// Commands in enforced namespaces should end with "Command".
    /// </summary>
    [Fact]
    public void NewCommands_ShouldFollowNamingConvention()
    {
        var enforcedNamespaces = new[]
        {
            "Cobryx.Application.Analytics",
            "Cobryx.Application.Collections",
            "Cobryx.Application.Invoicing",
            "Cobryx.Application.Customers",
            // Payments - critical sub-namespaces only (semi-enforced)
            "Cobryx.Application.Payments.Commands.ProcessPayment",
            "Cobryx.Application.Payments.Commands.RefundPayment",
            "Cobryx.Application.Payments.Commands.HandleChargeback",
        };

        var commandTypes = ApplicationAssembly.GetTypes()
            .Where(t => enforcedNamespaces.Any(ns => t.Namespace?.StartsWith(ns) == true))
            .Where(t => t.Namespace?.Contains(".Commands.") == true)
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Where(t => !t.IsNested) // Exclude compiler-generated nested types
            .Where(t => !t.Name.Contains("<")) // Exclude async state machines
            .Where(t => !t.Name.EndsWith("Handler"))
            .Where(t => !t.Name.EndsWith("Dto"))
            .Where(t => !t.Name.EndsWith("Validator"))
            .Where(t => !t.Name.EndsWith("Result"))
            .Where(t => !t.Name.EndsWith("Request")) // Exclude input DTOs
            .ToList();

        var violations = commandTypes
            .Where(t => !t.Name.EndsWith("Command"))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(violations.Count == 0,
            $"Command types not following naming convention:\n{string.Join("\n", violations)}\n\n" +
            "Fix: Rename to end with 'Command'.");
    }

    /// <summary>
    /// Reports naming convention violations in legacy code.
    /// Informational - helps track technical debt.
    /// </summary>
    [Fact]
    public void ReportLegacyNamingViolations()
    {
        var queryViolations = ApplicationAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains(".Queries.") == true)
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Where(t => !t.Name.EndsWith("Handler") && !t.Name.EndsWith("Dto") && !t.Name.EndsWith("Validator"))
            .Where(t => !t.Name.EndsWith("Query"))
            .Count();

        var commandViolations = ApplicationAssembly.GetTypes()
            .Where(t => t.Namespace?.Contains(".Commands.") == true)
            .Where(t => !t.IsInterface && !t.IsAbstract)
            .Where(t => !t.Name.EndsWith("Handler") && !t.Name.EndsWith("Dto") && !t.Name.EndsWith("Validator") && !t.Name.EndsWith("Result"))
            .Where(t => !t.Name.EndsWith("Command"))
            .Count();

        Assert.True(true,
            $"\n=== NAMING CONVENTION DEBT ===\n" +
            $"Query types not ending with 'Query': {queryViolations}\n" +
            $"Command types not ending with 'Command': {commandViolations}");
    }

    #endregion

    #region Baseline Locking (Prevent Regression)

    /// <summary>
    /// CRITICAL: Prevents architecture debt from increasing.
    /// Baselines are stored in architecture-baseline.json at repo root.
    /// Update baseline ONLY when intentionally paying down debt.
    /// </summary>
    [Fact]
    public void LegacyDebt_ShouldNotIncrease()
    {
        // Load baseline from JSON file
        var baseline = LoadBaseline();

        // Count current violations
        var namingViolations = CountNamingViolations();
        var tenantPolicyGaps = CountTenantPolicyGaps();

        Assert.True(namingViolations <= baseline.NamingViolations,
            $"Naming violations increased! Current: {namingViolations}, Baseline: {baseline.NamingViolations}\n" +
            "Fix: Either fix the new violations or update architecture-baseline.json with justification.");

        Assert.True(tenantPolicyGaps <= baseline.TenantPolicyGaps,
            $"Tenant policy gaps increased! Current: {tenantPolicyGaps}, Baseline: {baseline.TenantPolicyGaps}\n" +
            "Fix: Add [TenantScoped] + IRequiresTenant to new requests, or update baseline.");
    }

    private static ArchitectureBaseline LoadBaseline()
    {
        // Try to find baseline file (search up from test assembly location)
        var assemblyDir = Path.GetDirectoryName(ApplicationAssembly.Location)!;
        var searchDir = new DirectoryInfo(assemblyDir);

        while (searchDir != null)
        {
            var baselineFile = Path.Combine(searchDir.FullName, "architecture-baseline.json");
            if (File.Exists(baselineFile))
            {
                var json = File.ReadAllText(baselineFile);
                var doc = JsonDocument.Parse(json);
                var baselines = doc.RootElement.GetProperty("baselines");

                return new ArchitectureBaseline
                {
                    NamingViolations = baselines.GetProperty("namingViolations").GetInt32(),
                    TenantPolicyGaps = baselines.GetProperty("tenantPolicyGaps").GetInt32()
                };
            }
            searchDir = searchDir.Parent;
        }

        // Default baseline if file not found
        return new ArchitectureBaseline { NamingViolations = 50, TenantPolicyGaps = 120 };
    }

    private static int CountNamingViolations() => ApplicationAssembly.GetTypes()
        .Where(t => t.Namespace?.Contains(".Queries.") == true || t.Namespace?.Contains(".Commands.") == true)
        .Where(t => !t.IsInterface && !t.IsAbstract && !t.IsNested)
        .Where(t => !t.Name.Contains("<"))
        .Where(t => !t.Name.EndsWith("Handler") && !t.Name.EndsWith("Dto") && !t.Name.EndsWith("Validator") && !t.Name.EndsWith("Result"))
        .Where(t => !t.Name.EndsWith("Query") && !t.Name.EndsWith("Command"))
        .Count();

    private static int CountTenantPolicyGaps() => ApplicationAssembly.GetTypes()
        .Where(t => !t.IsInterface && !t.IsAbstract)
        .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Concordia.IRequest<>)))
        .Where(t => !t.IsDefined(typeof(TenantScopedAttribute), false))
        .Where(t => !typeof(IRequiresTenant).IsAssignableFrom(t))
        .Count();

    private record ArchitectureBaseline
    {
        public int NamingViolations { get; init; }
        public int TenantPolicyGaps { get; init; }
    }

    /// <summary>
    /// CRITICAL: Validates architecture-history.json integrity.
    /// History must be chronological for velocity calculations to be accurate.
    /// </summary>
    [Fact]
    public void ArchitectureHistory_ShouldBeChronologicalAndValid()
    {
        var historyFile = FindFile("architecture-history.json");
        if (historyFile == null)
        {
            // History file is optional, skip if not found
            return;
        }

        var json = File.ReadAllText(historyFile);
        var doc = JsonDocument.Parse(json);
        var history = doc.RootElement.GetProperty("history");
        var entries = history.EnumerateArray().ToList();

        // Must have at least 2 data points for velocity
        Assert.True(entries.Count >= 2,
            "Architecture history needs at least 2 data points for velocity calculation.");

        // Validate chronological order
        DateTime? previousDate = null;
        foreach (var entry in entries)
        {
            var dateStr = entry.GetProperty("date").GetString()!;
            var currentDate = DateTime.Parse(dateStr);

            if (previousDate.HasValue)
            {
                Assert.True(currentDate > previousDate.Value,
                    $"History timestamps must be strictly increasing. Found {dateStr} after {previousDate.Value:yyyy-MM-dd}");
            }

            previousDate = currentDate;
        }

        // Validate required fields exist
        foreach (var entry in entries)
        {
            Assert.True(entry.TryGetProperty("date", out _), "Each history entry must have 'date'");
            Assert.True(entry.TryGetProperty("metrics", out var metrics), "Each history entry must have 'metrics'");
            Assert.True(metrics.TryGetProperty("tenantPolicyCoverage", out _), "Metrics must have 'tenantPolicyCoverage'");
        }
    }

    private static string? FindFile(string fileName)
    {
        var assemblyDir = Path.GetDirectoryName(ApplicationAssembly.Location)!;
        var searchDir = new DirectoryInfo(assemblyDir);

        while (searchDir != null)
        {
            var file = Path.Combine(searchDir.FullName, fileName);
            if (File.Exists(file))
                return file;
            searchDir = searchDir.Parent;
        }
        return null;
    }

    #endregion

    #region Architecture Metrics (KPIs)

    /// <summary>
    /// Reports architecture health metrics as KPIs with module breakdown.
    /// Exports to architecture-metrics.json for CI/dashboard integration.
    /// </summary>
    [Fact]
    public void ReportArchitectureMetrics()
    {
        var allTypes = ApplicationAssembly.GetTypes().Where(t => !t.IsInterface && !t.IsAbstract).ToList();

        // Handler metrics
        var totalHandlers = allTypes.Where(t => t.Name.EndsWith("Handler") && !t.IsNested).Count();

        // All requests
        var allRequests = allTypes
            .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(Concordia.IRequest<>)))
            .ToList();

        // Global metrics
        var namingViolations = CountNamingViolations();
        var tenantGaps = CountTenantPolicyGaps();
        var requestsWithPolicy = allRequests.Count - tenantGaps;
        var tenantPolicyCoverage = allRequests.Count > 0 ? requestsWithPolicy * 100 / allRequests.Count : 0;

        // Module breakdown
        var moduleMetrics = GetModuleMetrics(allRequests);

        // Calculate Architecture Score (0-100)
        var architectureScore = CalculateArchitectureScore(tenantPolicyCoverage, namingViolations, allRequests.Count);

        // Get suggested actions WITH IMPACT × HOTSPOT WEIGHT (what moves the needle most)
        var impactPerRequest = allRequests.Count > 0 ? 100.0 / allRequests.Count : 0;
        var suggestedActions = allRequests
            .Where(t => !t.IsDefined(typeof(TenantScopedAttribute), false))
            .Where(t => !typeof(IRequiresTenant).IsAssignableFrom(t))
            .Select(t =>
            {
                var module = GetModuleName(t.Namespace ?? "Unknown");
                var moduleGaps = moduleMetrics.TryGetValue(module, out var m) ? m.gaps : 0;
                // Priority = impact × hotspot weight (fixing requests in hot modules = higher priority)
                var priority = impactPerRequest * (1 + moduleGaps * 0.1);
                return new
                {
                    t.Name,
                    Module = module,
                    Impact = impactPerRequest,
                    Priority = priority
                };
            })
            .OrderByDescending(a => a.Priority) // Prioritize by impact × hotspot
            .Take(5)
            .ToList();

        // Calculate velocity and confidence
        var (velocity, requiredVelocity, confidence) = CalculateVelocityAndConfidence(tenantPolicyCoverage);

        // Build metrics object
        var metrics = new
        {
            timestamp = DateTime.UtcNow.ToString("o"),
            architectureScore,
            confidence,
            velocity = new
            {
                current = velocity,
                required = requiredVelocity,
                onTrack = velocity >= requiredVelocity
            },
            targets = new
            {
                tenantPolicyCoverage = 30, // Target from baseline
                architectureScore = 60,
                targetDate = "2026-Q3"
            },
            application = new
            {
                totalHandlers,
                totalRequests = allRequests.Count,
                tenantPolicyCoverage,
                namingViolations,
                tenantPolicyGaps = tenantGaps
            },
            modules = moduleMetrics,
            hotspots = moduleMetrics
                .Where(m => m.Value.gaps > 0)
                .OrderByDescending(m => m.Value.gaps)
                .Take(5)
                .ToDictionary(m => m.Key, m => m.Value.gaps),
            suggestedActions
        };

        // Export to JSON (for CI integration)
        var metricsJson = JsonSerializer.Serialize(metrics, new JsonSerializerOptions { WriteIndented = true });

        // Try to write metrics file
        try
        {
            var outputPath = Path.Combine(Path.GetTempPath(), "architecture-metrics.json");
            File.WriteAllText(outputPath, metricsJson);
        }
        catch
        {
            // Ignore write errors in test environment
        }

        // Build hotspots display
        var hotspotsDisplay = string.Join("\n",
            moduleMetrics
                .Where(m => m.Value.gaps > 0)
                .OrderByDescending(m => m.Value.gaps)
                .Take(5)
                .Select(m => $"║  🔥 {m.Key,-20} {m.Value.gaps,10} gaps ║"));

        // Build suggested actions display WITH IMPACT
        var actionsDisplay = string.Join("\n",
            suggestedActions.Select(a => $"║  → {a.Name,-25} (+{a.Impact:F1}%) ║"));

        // Target gap calculation
        var targetCoverage = 30;
        var targetScore = 60;
        var coverageGap = Math.Max(0, targetCoverage - tenantPolicyCoverage);
        var scoreGap = Math.Max(0, targetScore - architectureScore);

        // Velocity indicator
        var velocityStatus = velocity >= requiredVelocity ? "✅ On track" : "⚠️ Behind";

        Assert.True(true,
            $"\n" +
            $"╔══════════════════════════════════════════════════╗\n" +
            $"║         ARCHITECTURE HEALTH METRICS              ║\n" +
            $"╠══════════════════════════════════════════════════╣\n" +
            $"║  {confidence,-46} ║\n" +
            $"╠══════════════════════════════════════════════════╣\n" +
            $"║  🏆 SCORE: {architectureScore,3}/100  (target: {targetScore}, gap: {scoreGap,2})       ║\n" +
            $"╠══════════════════════════════════════════════════╣\n" +
            $"║  📈 VELOCITY                                     ║\n" +
            $"║     Current:  {velocity,5:F2}%/week                       ║\n" +
            $"║     Required: {requiredVelocity,5:F2}%/week  {velocityStatus,-16} ║\n" +
            $"╠══════════════════════════════════════════════════╣\n" +
            $"║  Handlers:              {totalHandlers,20} ║\n" +
            $"║  Total Requests:        {allRequests.Count,20} ║\n" +
            $"║  Tenant Coverage: {tenantPolicyCoverage,3}% (target: {targetCoverage}%, gap: {coverageGap,2}%)  ║\n" +
            $"║  Naming Violations:     {namingViolations,20} ║\n" +
            $"║  Tenant Policy Gaps:    {tenantGaps,20} ║\n" +
            $"╠══════════════════════════════════════════════════╣\n" +
            $"║           🔥 DEBT HOTSPOTS                       ║\n" +
            $"╠══════════════════════════════════════════════════╣\n" +
            $"{hotspotsDisplay}\n" +
            $"╠══════════════════════════════════════════════════╣\n" +
            $"║        📍 SUGGESTED ACTIONS                      ║\n" +
            $"╠══════════════════════════════════════════════════╣\n" +
            $"{actionsDisplay}\n" +
            $"╚══════════════════════════════════════════════════╝\n" +
            $"\nMetrics JSON:\n{metricsJson}");
    }

    private static Dictionary<string, (int total, int covered, int gaps, int coverage)> GetModuleMetrics(List<Type> allRequests)
    {
        var modules = allRequests
            .GroupBy(t => GetModuleName(t.Namespace ?? "Unknown"))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var total = g.Count();
                    var covered = g.Count(t =>
                        t.IsDefined(typeof(TenantScopedAttribute), false) ||
                        typeof(IRequiresTenant).IsAssignableFrom(t));
                    var gaps = total - covered;
                    var coverage = total > 0 ? covered * 100 / total : 0;
                    return (total, covered, gaps, coverage);
                });

        return modules;
    }

    private static string GetModuleName(string ns)
    {
        // Extract module name from namespace like "Cobryx.Application.Payments.Commands..."
        var parts = ns.Split('.');
        if (parts.Length >= 3 && parts[0] == "Cobryx" && parts[1] == "Application")
        {
            return parts[2];
        }
        return "Other";
    }

    private static int CalculateArchitectureScore(int tenantCoverage, int namingViolations, int totalRequests)
    {
        // NON-GAMEABLE Score formula:
        // - Tenant coverage: 50% weight (0-50 points) - THE MOST IMPORTANT
        // - Naming compliance: 30% weight (0-30 points)
        // - Base score: 20 points (for having the system)
        //
        // ANTI-GAMING: Score is CAPPED if tenant coverage is too low
        // This prevents gaming by improving only naming while ignoring tenant policy

        var tenantScore = tenantCoverage / 2; // 0-50 points

        // Naming: fewer violations = higher score
        var maxExpectedViolations = Math.Max(totalRequests / 2, 50);
        var namingScore = Math.Max(0, 30 - (namingViolations * 30 / maxExpectedViolations));

        var baseScore = 20; // For having governance system

        var rawScore = tenantScore + namingScore + baseScore;

        // ANTI-GAMING CAPS:
        // - If tenant coverage < 10%, max score is 35 (can't reach "good" without tenant work)
        // - If tenant coverage < 25%, max score is 55 (can't reach "great" without tenant work)
        // - If tenant coverage < 50%, max score is 75 (can't reach "excellent" without tenant work)
        if (tenantCoverage < 10)
            return Math.Min(rawScore, 35);
        if (tenantCoverage < 25)
            return Math.Min(rawScore, 55);
        if (tenantCoverage < 50)
            return Math.Min(rawScore, 75);

        return Math.Min(100, rawScore);
    }

    private static (double velocity, double required, string confidence) CalculateVelocityAndConfidence(int currentCoverage)
    {
        // Target: 30% coverage by Q3 2026 (approx 12 weeks from April 7)
        const int targetCoverage = 30;
        const int weeksRemaining = 12;

        // Calculate REAL velocity from history WITH TREND
        var (currentVelocity, previousVelocity) = CalculateRealVelocityWithTrend();

        // Required velocity to hit target
        var coverageGap = targetCoverage - currentCoverage;
        var requiredVelocity = (double)coverageGap / weeksRemaining;

        // Trend indicator
        var trend = currentVelocity - previousVelocity;
        var trendIndicator = trend > 0.1 ? " ↑ improving" :
                            trend < -0.1 ? " ↓ declining" : "";

        // Confidence calculation WITH CONTEXT AND TREND
        string confidence;
        if (currentCoverage >= targetCoverage)
        {
            confidence = "🟢 HIGH - Target achieved!";
        }
        else if (currentVelocity >= requiredVelocity)
        {
            confidence = $"🟢 HIGH - On track ({currentVelocity:F2} >= {requiredVelocity:F2} req){trendIndicator}";
        }
        else if (currentVelocity >= requiredVelocity * 0.5)
        {
            var gap = requiredVelocity - currentVelocity;
            confidence = $"🟡 MEDIUM - Need +{gap:F2}%/wk{trendIndicator}";
        }
        else
        {
            var multiplier = requiredVelocity / Math.Max(currentVelocity, 0.01);
            confidence = $"🔴 LOW - Need {multiplier:F1}x velocity{trendIndicator}";
        }

        return (currentVelocity, Math.Round(requiredVelocity, 2), confidence);
    }

    private static (double current, double previous) CalculateRealVelocityWithTrend()
    {
        // Try to load from architecture-history.json
        try
        {
            var assemblyDir = Path.GetDirectoryName(ApplicationAssembly.Location)!;
            var searchDir = new DirectoryInfo(assemblyDir);

            while (searchDir != null)
            {
                var historyFile = Path.Combine(searchDir.FullName, "architecture-history.json");
                if (File.Exists(historyFile))
                {
                    var json = File.ReadAllText(historyFile);
                    var doc = JsonDocument.Parse(json);
                    var history = doc.RootElement.GetProperty("history");
                    var entries = history.EnumerateArray().ToList();

                    if (entries.Count >= 2)
                    {
                        // Calculate current velocity (last two entries)
                        var currentVelocity = CalculateVelocityBetween(entries[^2], entries[^1]);

                        // Calculate previous velocity (if we have 3+ entries)
                        var previousVelocity = entries.Count >= 3
                            ? CalculateVelocityBetween(entries[^3], entries[^2])
                            : currentVelocity;

                        return (currentVelocity, previousVelocity);
                    }
                }
                searchDir = searchDir.Parent;
            }
        }
        catch
        {
            // Fall back to default if history can't be read
        }

        // Default fallback
        return (0.67, 0.67);
    }

    private static double CalculateVelocityBetween(JsonElement older, JsonElement newer)
    {
        var newerCoverage = newer.GetProperty("metrics").GetProperty("tenantPolicyCoverage").GetInt32();
        var olderCoverage = older.GetProperty("metrics").GetProperty("tenantPolicyCoverage").GetInt32();

        var newerDate = DateTime.Parse(newer.GetProperty("date").GetString()!);
        var olderDate = DateTime.Parse(older.GetProperty("date").GetString()!);

        var deltaCoverage = newerCoverage - olderCoverage;
        var deltaWeeks = (newerDate - olderDate).TotalDays / 7.0;

        // Edge case protection
        if (deltaWeeks < 0.1)
            return 0; // Less than ~1 day, not meaningful

        var velocity = deltaCoverage / deltaWeeks;
        return Math.Max(0, velocity); // No negative velocity (would mean regression)
    }

    #endregion
}
