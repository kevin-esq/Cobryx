using NetArchTest.Rules;

namespace Cobryx.ArchitectureTests;

public class CleanArchitectureTests
{
    private const string DomainNamespace = "Cobryx.Domain";
    private const string ApplicationNamespace = "Cobryx.Application";
    private const string InfrastructureNamespace = "Cobryx.Infrastructure";
    private const string ApiNamespace = "Cobryx.Api";

    private static string FormatError(TestResult result)
    {
        return result.IsSuccessful
            ? string.Empty
            : $"Violations found in: {string.Join(", ", result.FailingTypeNames ?? [])}";
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Application_Infrastructure_Or_Api()
    {
        var result = Types
            .InAssembly(typeof(Domain.Shared.BaseEntity).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                InfrastructureNamespace,
                ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatError(result));
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var result = Types
            .InAssembly(typeof(Application.Common.Interfaces.ICobryxDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                InfrastructureNamespace,
                ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatError(result));
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var result = Types
            .InAssembly(typeof(Cobryx.Infrastructure.DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatError(result));
    }

    [Fact]
    public void Controllers_Should_Not_Depend_On_Infrastructure()
    {
        var result = Types
            .InNamespace($"{ApiNamespace}.Controllers")
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatError(result));
    }

    [Fact]
    public void Domain_Entities_Should_Not_Depend_On_EfCore()
    {
        var result = Types
            .InAssembly(typeof(Cobryx.Domain.Shared.BaseEntity).Assembly)
            .That()
            .ResideInNamespace($"{DomainNamespace}")
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatError(result));
    }

    [Fact]
    public void Repositories_Should_Reside_In_Infrastructure()
    {
        var result = Types
            .InCurrentDomain()
            .That()
            .HaveNameEndingWith("Repository")
            .Should()
            .ResideInNamespace(InfrastructureNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, FormatError(result));
    }

    [Fact]
    public void Interfaces_Should_Start_With_I()
    {
        var result = Types
            .InCurrentDomain()
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
        var result = Types
            .InCurrentDomain()
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
        var result = Types
            .InCurrentDomain()
            .That()
            .HaveNameEndingWith("Module")
            .Should()
            .ResideInNamespaceMatching(
                $"{ApplicationNamespace}.*|{InfrastructureNamespace}.*")
            .GetResult();

        Assert.True(result.IsSuccessful, FormatError(result));
    }
}