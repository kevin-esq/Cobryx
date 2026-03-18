using Cobryx.Domain.Shared;

using FluentAssertions;

namespace Cobryx.IntegrationTests;

public class ArchitectureTests
{
    [Fact]
    public void AllCustomExceptions_MustInheritFromCobryxException()
    {
        var domainAssembly = typeof(CobryxException).Assembly;

        var exceptionTypes = domainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Exception"))
            .ToList();

        foreach (var type in exceptionTypes)
        {
            if (type.Name == "DomainException" || type.Name == "CobryxException")
                continue;

            type.Should().BeAssignableTo<CobryxException>(
                $"Exception type {type.Name} must inherit from CobryxException to ensure Zero-Text compliance.");
        }
    }

    [Fact]
    public void CobryxExceptionSubclasses_ShouldNotHaveMessageConstructors()
    {
        var assemblies = new[]
        {
            typeof(CobryxException).Assembly,
        };

        foreach (var assembly in assemblies)
        {
            var exceptionTypes = assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(CobryxException)) && !t.IsAbstract);

            foreach (var type in exceptionTypes)
            {
                var constructors = type.GetConstructors();
                foreach (var constructor in constructors)
                {
                    var parameters = constructor.GetParameters();

                    parameters.Should().NotContain(p => p.Name!.Equals("message", StringComparison.OrdinalIgnoreCase),
                        $"Exception {type.Name} has a constructor with a 'message' parameter, violating the Zero-Text policy.");
                }
            }
        }
    }
}
