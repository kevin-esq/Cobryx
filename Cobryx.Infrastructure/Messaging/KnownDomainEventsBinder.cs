using System.Reflection;

using Cobryx.Domain.Shared;

using Newtonsoft.Json.Serialization;

namespace Cobryx.Infrastructure.Messaging;

/// <summary>
/// Security binder that only allows deserialization of known domain event types.
/// Prevents deserialization attacks via TypeNameHandling.
/// </summary>
public class KnownDomainEventsBinder : ISerializationBinder
{
    private static readonly HashSet<string> AllowedAssemblies = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cobryx.Domain",
        "Cobryx.Application"
    };

    private static readonly Dictionary<string, Type> KnownTypes;

    static KnownDomainEventsBinder()
    {
        KnownTypes = new Dictionary<string, Type>();

        // Scan domain assembly for all IDomainEvent implementations
        var domainAssembly = typeof(IDomainEvent).Assembly;
        foreach (var type in domainAssembly.GetTypes())
        {
            if (typeof(IDomainEvent).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            {
                var key = $"{type.FullName}, {type.Assembly.GetName().Name}";
                KnownTypes[key] = type;
                KnownTypes[type.FullName!] = type;
            }
        }
    }

    public Type BindToType(string? assemblyName, string typeName)
    {
        // Try full key first
        var fullKey = string.IsNullOrEmpty(assemblyName) ? typeName : $"{typeName}, {assemblyName}";

        if (KnownTypes.TryGetValue(fullKey, out var type))
            return type;

        if (KnownTypes.TryGetValue(typeName, out type))
            return type;

        // Validate assembly is allowed (domain payloads) or trusted BCL for generic collections etc.
        if (!string.IsNullOrEmpty(assemblyName) &&
            !AllowedAssemblies.Any(a => assemblyName.StartsWith(a, StringComparison.OrdinalIgnoreCase)) &&
            !assemblyName.StartsWith("System.Private.CoreLib", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Deserialization of type '{typeName}' from assembly '{assemblyName}' is not allowed.");
        }

        // Try to resolve from allowed assemblies
        foreach (var allowedAssembly in AllowedAssemblies)
        {
            try
            {
                var assembly = Assembly.Load(allowedAssembly);
                type = assembly.GetType(typeName);
                if (type != null && typeof(IDomainEvent).IsAssignableFrom(type))
                {
                    KnownTypes[fullKey] = type;
                    return type;
                }
            }
            catch
            {
                // Assembly not loaded, continue
            }
        }

        // Nested CLR types inside domain event payloads (e.g. Money on PaymentCompletedEvent) are not
        // IDomainEvent implementations but must still resolve when Newtonsoft follows $type metadata.
        if (!string.IsNullOrEmpty(assemblyName) &&
            AllowedAssemblies.Any(a => assemblyName.StartsWith(a, StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var asm = Assembly.Load(assemblyName.Split(',')[0].Trim());
                var nestedType = asm.GetType(typeName, throwOnError: false, ignoreCase: false);
                if (nestedType != null && nestedType.Assembly.GetName().Name is { } nestedAssembly &&
                    AllowedAssemblies.Contains(nestedAssembly))
                {
                    KnownTypes[fullKey] = nestedType;
                    return nestedType;
                }
            }
            catch
            {
                // fall through to error below
            }
        }

        // System.Private.CoreLib: closed generic lists and other BCL types referenced in JSON $type metadata.
        if (!string.IsNullOrEmpty(assemblyName) &&
            assemblyName.StartsWith("System.Private.CoreLib", StringComparison.OrdinalIgnoreCase))
        {
            var t = Type.GetType($"{typeName}, {assemblyName}", throwOnError: false, ignoreCase: false);
            if (t != null)
            {
                KnownTypes[fullKey] = t;
                return t;
            }
        }

        throw new InvalidOperationException($"Unknown or disallowed type: {typeName}");
    }

    public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
    {
        assemblyName = serializedType.Assembly.GetName().Name;
        typeName = serializedType.FullName;
    }
}
