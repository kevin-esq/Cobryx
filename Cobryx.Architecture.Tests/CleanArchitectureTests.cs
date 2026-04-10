using NetArchTest.Rules;

namespace Cobryx.Architecture.Tests;

public class CleanArchitectureTests
{
    private const string DomainNamespace = "Cobryx.Domain";
    private const string ApplicationNamespace = "Cobryx.Application";
    private const string InfrastructureNamespace = "Cobryx.Infrastructure";
    private const string ApiNamespace = "Cobryx.Api";

    private static readonly System.Reflection.Assembly DomainAssembly = typeof(Domain.Shared.BaseEntity).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(Application.Common.Interfaces.ICobryxDbContext).Assembly;
    private static readonly System.Reflection.Assembly InfrastructureAssembly = typeof(Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_Should_Not_Depend_On_Application_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNamespace, InfrastructureNamespace, ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatError(result));
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .That()
            .DoNotHaveName("CobryxMetrics")
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNamespace, ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatError(result));
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatError(result));
    }

    [Fact]
    public void Controllers_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types.InNamespace($"{ApiNamespace}.Controllers")
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatError(result));
    }

    [Fact]
    public void Domain_Entities_Should_Not_Depend_On_EfCore()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .ResideInNamespace(DomainNamespace)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatError(result));
    }

    [Fact]
    public void Repositories_Should_Reside_In_Infrastructure()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Repository")
            .Should()
            .ResideInNamespace(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatError(result));
    }

    [Fact]
    public void Interfaces_Should_Start_With_I()
    {
        var result = Types.InCurrentDomain()
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatError(result));
    }

    [Fact]
    public void Domain_Events_Should_Be_In_Domain_Layer()
    {
        var result = Types.InAssembly(DomainAssembly)
            .That()
            .HaveNameEndingWith("Event")
            .Should()
            .ResideInNamespace(DomainNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatError(result));
    }

    [Fact]
    public void Modules_Should_Exist_In_Application_And_Infrastructure()
    {
        var result = Types.InCurrentDomain()
            .That()
            .AreClasses()
            .And()
            .HaveNameEndingWith("Module")
            .And()
            .DoNotResideInNamespace(DomainNamespace)
            .Should()
            .ResideInNamespaceMatching($"{ApplicationNamespace}.*|{InfrastructureNamespace}.*")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatError(result));
    }

    private static string FormatError(TestResult result) =>
        result.IsSuccessful ? string.Empty : $"Violations: {string.Join(", ", result.FailingTypeNames ?? [])}";
}
