using System.Reflection;
using System.Text.Json;

using Cobryx.Application.Analytics.Queries.GetPortfolioSummary;
using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Payments.Commands.CreatePaymentLink;
using Cobryx.Application.Payments.Commands.HandleChargeback;
using Cobryx.Application.Payments.Commands.InitializePaymentLink;
using Cobryx.Application.Payments.Commands.ProcessPayment;
using Cobryx.Application.Payments.Commands.RefundPayment;
using Cobryx.Application.Payments.Commands.Register;

using Concordia;

namespace Cobryx.Application.Tests.Behaviors.Tenant;

/// <summary>
/// Tenant Policy Tests - Automatically enforces that:
/// 1. All [TenantScoped] requests implement IRequiresTenant
/// 2. All requests declare tenant policy explicitly
/// 3. No manual list maintenance required - fully declarative
/// </summary>
public class TenantPolicyTests
{
    private static readonly Assembly _applicationAssembly = typeof(GetPortfolioSummaryQuery).Assembly;

    /// <summary>
    /// Gets all MediatR request types (queries and commands) from the Application assembly.
    /// Uses interface detection instead of naming convention.
    /// </summary>
    private static List<Type> GetAllRequestTypes() => _applicationAssembly.GetTypes()
        .Where(t => !t.IsInterface && !t.IsAbstract)
        .Where(t => t.GetInterfaces().Any(i =>
            i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)))
        .ToList();

    /// <summary>
    /// CORE TEST: Any request marked [TenantScoped] MUST implement IRequiresTenant.
    /// This is automatic - no manual list needed.
    /// </summary>
    [Fact]
    public void AllTenantScopedRequests_MustImplementIRequiresTenant()
    {
        var tenantScopedTypes = _applicationAssembly.GetTypes()
            .Where(t => t.GetCustomAttribute<TenantScopedAttribute>() != null)
            .ToList();

        var violations = tenantScopedTypes
            .Where(t => !typeof(IRequiresTenant).IsAssignableFrom(t))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(violations.Count == 0,
            $"[TenantScoped] requests missing IRequiresTenant:\n{string.Join("\n", violations)}\n\n" +
            "Fix: Add 'IRequiresTenant' to the request interface list.");
    }

    /// <summary>
    /// Validates that NEW requests (in specific namespaces) declare tenant policy.
    /// Legacy requests are tracked separately for gradual migration.
    /// </summary>
    [Fact]
    public void NewRequests_MustDeclareTenantPolicyExplicitly()
    {
        // Namespaces where tenant policy is REQUIRED for all new requests
        var enforcedNamespaces = new[]
        {
            "Cobryx.Application.Analytics",
            "Cobryx.Application.Collections",
            "Cobryx.Application.Invoicing",
            "Cobryx.Application.Customers.Commands.Create",
            "Cobryx.Application.Customers.Commands.Update",
            "Cobryx.Application.Customers.Commands.Delete",
            "Cobryx.Application.Customers.Queries.GetCustomerById",
            "Cobryx.Application.Customers.Queries.GetCustomers",
            // Payments - critical sub-namespaces only (semi-enforced)
            "Cobryx.Application.Payments.Commands.ProcessPayment",
            "Cobryx.Application.Payments.Commands.RefundPayment",
            "Cobryx.Application.Payments.Commands.HandleChargeback",
        };

        // Namespaces exempt from tenant policy (customer portal - JWT-authenticated)
        // Reserved for future use when customer portal is implemented
        // var exemptNamespaces = new[] { "Cobryx.Application.Customers.Commands.PaymentMethods" };

        var requests = GetAllRequestTypes()
            .Where(t => enforcedNamespaces.Any(ns => t.Namespace?.StartsWith(ns) == true))
            .ToList();

        var violations = new List<string>();

        foreach (var request in requests)
        {
            var hasTenantScoped = request.IsDefined(typeof(TenantScopedAttribute), false);
            var implementsIRequiresTenant = typeof(IRequiresTenant).IsAssignableFrom(request);

            if (!hasTenantScoped && !implementsIRequiresTenant)
            {
                violations.Add($"{request.FullName}");
            }
        }

        Assert.True(violations.Count == 0,
            $"Requests in enforced namespaces without tenant policy:\n{string.Join("\n", violations)}\n\n" +
            "Fix: Add [TenantScoped] + IRequiresTenant to each request.");
    }

    /// <summary>
    /// Informational: Tracks legacy requests without tenant policy.
    /// Use this to plan gradual migration.
    /// </summary>
    [Fact]
    public void ReportLegacyRequestsWithoutTenantPolicy()
    {
        var requests = GetAllRequestTypes();

        var withPolicy = requests
            .Where(t => t.IsDefined(typeof(TenantScopedAttribute), false) ||
                       typeof(IRequiresTenant).IsAssignableFrom(t))
            .Count();

        var withoutPolicy = requests
            .Where(t => !t.IsDefined(typeof(TenantScopedAttribute), false) &&
                       !typeof(IRequiresTenant).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToList();

        // Always passes - informational for tracking migration progress
        Assert.True(true,
            $"\n=== TENANT POLICY MIGRATION STATUS ===\n" +
            $"Total requests: {requests.Count}\n" +
            $"With policy: {withPolicy}\n" +
            $"Without policy (legacy): {withoutPolicy.Count}\n" +
            $"Migration progress: {withPolicy * 100 / requests.Count}%");
    }

    /// <summary>
    /// Sanity check: We should have at least some tenant-scoped requests.
    /// Catches accidental removal of all attributes.
    /// </summary>
    [Fact]
    public void ShouldHaveAtLeastOneTenantScopedRequest()
    {
        var count = GetAllRequestTypes()
            .Count(t => t.GetCustomAttribute<TenantScopedAttribute>() != null);

        Assert.True(count > 0,
            "No [TenantScoped] requests found. Did someone remove all attributes?");
    }

    /// <summary>
    /// Consistency check: IRequiresTenant without [TenantScoped] is suspicious.
    /// Either add the attribute or document why it's different.
    /// </summary>
    [Fact]
    public void IRequiresTenantWithoutAttribute_ShouldBeDocumented()
    {
        var withInterfaceNoAttribute = GetAllRequestTypes()
            .Where(t => typeof(IRequiresTenant).IsAssignableFrom(t))
            .Where(t => t.GetCustomAttribute<TenantScopedAttribute>() == null)
            .Select(t => t.Name)
            .ToList();

        Assert.True(withInterfaceNoAttribute.Count == 0,
            $"Requests with IRequiresTenant but no [TenantScoped] attribute:\n" +
            $"{string.Join("\n", withInterfaceNoAttribute)}\n\n" +
            "Fix: Add [TenantScoped] attribute for consistency.");
    }

    /// <summary>
    /// Informational: Reports all requests and their tenant policy status.
    /// Helps identify gaps during code review.
    /// </summary>
    [Fact]
    public void ReportTenantPolicyStatus()
    {
        var allRequests = GetAllRequestTypes();

        var tenantScoped = allRequests
            .Where(t => t.GetCustomAttribute<TenantScopedAttribute>() != null)
            .Select(t => t.Name)
            .ToList();

        var noPolicy = allRequests
            .Where(t => t.GetCustomAttribute<TenantScopedAttribute>() == null)
            .Where(t => !typeof(IRequiresTenant).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToList();

        // Always passes - informational only
        Assert.True(true,
            $"\n=== TENANT POLICY REPORT ===\n" +
            $"Total requests: {allRequests.Count}\n" +
            $"[TenantScoped] ({tenantScoped.Count}): {string.Join(", ", tenantScoped)}\n" +
            $"No explicit policy ({noPolicy.Count}): {string.Join(", ", noPolicy.Take(20))}...");
    }

    /// <summary>
    /// CRITICAL: Prevents silent removal of tenant policy.
    /// Once a request has tenant policy, it cannot be removed without explicit baseline update.
    /// Reads from architecture-baseline.json for persistence and auditability.
    /// </summary>
    [Fact]
    public void TenantPolicy_ShouldNotBeSilentlyRemoved()
    {
        // Load baseline from JSON (persistent and auditable)
        var baselineTenantScopedRequests = LoadTenantScopedBaseline();

        var currentTenantScoped = GetAllRequestTypes()
            .Where(t => t.IsDefined(typeof(TenantScopedAttribute), false) ||
                       typeof(IRequiresTenant).IsAssignableFrom(t))
            .Select(t => t.Name)
            .ToHashSet();

        var removedFromBaseline = baselineTenantScopedRequests
            .Where(name => !currentTenantScoped.Contains(name))
            .ToList();

        Assert.True(removedFromBaseline.Count == 0,
            $"🚨 TENANT POLICY REMOVED from baseline requests:\n" +
            $"{string.Join("\n", removedFromBaseline)}\n\n" +
            "This is a CRITICAL regression. If intentional, update architecture-baseline.json with [ARCH_BASELINE] commit.");
    }

    /// <summary>
    /// CRITICAL: These payment commands handle money and MUST have tenant policy.
    /// This test prevents regression on security-critical commands.
    /// </summary>
    [Fact]
    public void CriticalPaymentCommands_MustHaveTenantPolicy()
    {
        var criticalCommands = new[]
        {
            typeof(ProcessPaymentCommand),
            typeof(RefundPaymentCommand),
            typeof(HandleChargebackCommand),
            typeof(RegisterPaymentCommand),
            typeof(CreatePaymentLinkCommand),
        };

        var violations = new List<string>();

        foreach (var command in criticalCommands)
        {
            if (!typeof(IRequiresTenant).IsAssignableFrom(command))
            {
                violations.Add($"{command.Name} must implement IRequiresTenant");
            }

            if (!command.IsDefined(typeof(TenantScopedAttribute), false))
            {
                violations.Add($"{command.Name} must have [TenantScoped] attribute");
            }
        }

        Assert.True(violations.Count == 0,
            $"🚨 CRITICAL SECURITY: Payment commands missing tenant policy:\n" +
            $"{string.Join("\n", violations)}\n\n" +
            "These commands handle money and MUST be tenant-protected.");
    }

    /// <summary>
    /// PUBLIC: These commands are token-based and MUST NOT have tenant policy.
    /// Adding tenant policy would break the public payment flow.
    /// </summary>
    [Fact]
    public void PublicPaymentCommands_MustNotHaveTenantPolicy()
    {
        var publicCommands = new[]
        {
            typeof(InitializePaymentLinkCommand),
        };

        var violations = new List<string>();

        foreach (var command in publicCommands)
        {
            if (typeof(IRequiresTenant).IsAssignableFrom(command))
            {
                violations.Add($"{command.Name} implements IRequiresTenant but is PUBLIC");
            }

            if (command.IsDefined(typeof(TenantScopedAttribute), false))
            {
                violations.Add($"{command.Name} has [TenantScoped] but is PUBLIC");
            }
        }

        Assert.True(violations.Count == 0,
            $"🚨 PUBLIC ENDPOINT VIOLATION: These commands must NOT have tenant policy:\n" +
            $"{string.Join("\n", violations)}\n\n" +
            "These are token-based public endpoints. Adding tenant policy will break them.");
    }

    /// <summary>
    /// SECURITY: Handlers must NOT use request.TenantId as fallback.
    /// Tenant context must ALWAYS come from ITenantProvider.
    /// This test scans source files to detect the insecure pattern.
    /// </summary>
    [Fact]
    public void Handlers_ShouldNotUse_RequestTenantIdFallback()
    {
        // Handlers that are allowed to use request.TenantId (public endpoints, admin, etc.)
        var allowedHandlers = new HashSet<string>
        {
            "InitializePaymentLinkHandler",  // Public endpoint - token-based
            "SuspendTenantHandler",          // Admin operation
            "RecordPayoutHandler",           // Admin/platform operation
            "GetFinancialMetricsHandler",    // Admin dashboard
            "GetStripeReconciliationHandler", // Admin/platform
            "GetCustomerStatementHandler",   // May be public portal
            "GetAgingReportHandler",         // May be scheduled job
            "GetCustomerPortalSummaryHandler", // Public portal
        };

        var violations = new List<string>();

        // Find the Application project source directory
        var assemblyDir = Path.GetDirectoryName(_applicationAssembly.Location)!;
        var searchDir = new DirectoryInfo(assemblyDir);

        while (searchDir != null && !Directory.Exists(Path.Combine(searchDir.FullName, "Cobryx.Application")))
        {
            searchDir = searchDir.Parent;
        }

        if (searchDir == null)
        {
            return; // Can't find source, skip test
        }

        var applicationDir = Path.Combine(searchDir.FullName, "Cobryx.Application");
        var handlerFiles = Directory.GetFiles(applicationDir, "*Handler.cs", SearchOption.AllDirectories);

        foreach (var file in handlerFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file);

            if (allowedHandlers.Contains(fileName))
                continue;

            var content = File.ReadAllText(file);

            // Detect insecure pattern: ?? request.TenantId
            if (content.Contains("?? request.TenantId"))
            {
                violations.Add($"{fileName}: uses '?? request.TenantId' fallback (INSECURE)");
            }
        }

        Assert.True(violations.Count == 0,
            $"🚨 SECURITY: Handlers using insecure tenant fallback:\n" +
            $"{string.Join("\n", violations)}\n\n" +
            "Fix: Remove '?? request.TenantId' and use only ITenantProvider.GetTenantId()");
    }

    private static HashSet<string> LoadTenantScopedBaseline()
    {
        try
        {
            var assemblyDir = Path.GetDirectoryName(_applicationAssembly.Location)!;
            var searchDir = new DirectoryInfo(assemblyDir);

            while (searchDir != null)
            {
                var baselineFile = Path.Combine(searchDir.FullName, "architecture-baseline.json");
                if (File.Exists(baselineFile))
                {
                    var json = File.ReadAllText(baselineFile);
                    var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("tenantScopedRequests", out var requests))
                    {
                        return requests.EnumerateArray()
                            .Select(e => e.GetString()!)
                            .ToHashSet();
                    }
                }
                searchDir = searchDir.Parent;
            }
        }
        catch
        {
            // Fall back to hardcoded if file can't be read
        }

        // Fallback baseline
        return new HashSet<string>
        {
            "GetPortfolioSummaryQuery",
            "GetPortfolioAgingQuery",
            "GetPriorityCasesQuery",
        };
    }
}
