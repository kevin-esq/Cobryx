using System.Reflection;

using Cobryx.Application.Common.Interfaces;

using Microsoft.Extensions.Configuration;

using NetArchTest.Rules;

using Xunit.Abstractions;

namespace Cobryx.Architecture.Tests;

public class StandardComplianceTests(ITestOutputHelper output)
{
    private static readonly Assembly ApplicationAssembly = typeof(IClock).Assembly;
    private static readonly Assembly DomainAssembly = typeof(Domain.Shared.BaseEntity).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Report_DateTimeUtcNow_Violations()
    {
        var result = Types.InAssemblies([ApplicationAssembly, DomainAssembly])
            .That()
            .DoNotHaveNameMatching(".*Interceptor.*")
            .And().DoNotHaveNameMatching(".*Clock.*")
            .Should()
            .HaveDependencyOn("System.DateTime")
            .GetResult();

        Report("DateTime.UtcNow", "TIME-DEBT", result);
    }

    [Fact]
    public void Report_IConfiguration_In_Application_Violations()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .ResideInNamespaceMatching("Cobryx.Application.*")
            .ShouldNot()
            .HaveDependencyOn(typeof(IConfiguration).Assembly.FullName)
            .GetResult();

        Report("IConfiguration Leaks", "CONFIG-DEBT", result);
    }

    [Fact]
    public void Report_Static_Logging_Violations()
    {
        var result = Types.InAssemblies([ApplicationAssembly, InfrastructureAssembly])
            .That()
            .DoNotHaveNameMatching(".*Program.*")
            .And().DoNotHaveNameMatching(".*Startup.*")
            .And().DoNotHaveNameMatching(".*DependencyInjection.*")
            .ShouldNot()
            .HaveDependencyOn("Serilog")
            .GetResult();

        Report("Static Logging (Serilog)", "LOGGING-DEBT", result);
    }

    private void Report(string title, string tag, TestResult result)
    {
        var violations = result.FailingTypeNames?.ToList() ?? [];
        output.WriteLine($"--- {title} Compliance Report ---");
        output.WriteLine($"Violations: {violations.Count}");
        foreach (var type in violations)
            output.WriteLine($"  [{tag}] {type}");
    }
}
