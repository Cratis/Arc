// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator;

public static partial class TypeExtensions
{
    const string CommandResponseValueHandlerTypeName = "Cratis.Arc.Commands.ICommandResponseValueHandler`1";
    const string RuntimeCommandResponseValueHandlerTypeName = "Cratis.Arc.Commands.ICommandResponseValueHandler";
    const string CommandResponseValueHandlerContractsAssemblyName = "Cratis.Arc.Core";

    /// <summary>
    /// Check if a type is consumed by a declared server-side command response value handler.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns>True if the type is consumed on the server and should be ignored as a client response, false otherwise.</returns>
    public static bool IsWellKnownType(this Type type) => type.IsServerHandledCommandResponseValue();

    /// <summary>
    /// Check if a command response value type is consumed by a server-side command response value handler.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if the value is handled on the server and should not be exposed as a client response.</returns>
    public static bool IsServerHandledCommandResponseValue(this Type type)
    {
        // Match the owning Arc assembly as well as the name; MetadataLoadContext types cannot use runtime assignability.
        if (IsCommandOperationValue(type))
        {
            return true;
        }

        var isFromCurrentMetadataContext = _metadataLoadContext?.GetAssemblies().Contains(type.Assembly) == true;
        var handledTypeIdentities = isFromCurrentMetadataContext
            ? _serverHandledCommandResponseValueTypeNames ?? []
            : FindRuntimeServerHandledCommandResponseValueTypeNames();

        return EnumerateTypeAndContracts(type).Any(_ =>
            _.AssemblyQualifiedName is not null && handledTypeIdentities.Contains(_.AssemblyQualifiedName));
    }

    /// <summary>
    /// Detects bare operation collections inside command return shapes without generating descriptor models.
    /// </summary>
    /// <param name="type">The declared return shape.</param>
    /// <returns>Whether the explicit CommandOperations wrapper is required.</returns>
    public static bool ContainsBareCommandOperationCollection(this Type type)
    {
        if (type.FullName == "Cratis.Arc.Commands.CommandOperations" && IsCommandOperationContract(type))
        {
            return false;
        }

        var contracts = type.GetInterfaces().Append(type);
        if (contracts.Any(IsEnumerableOfCommandOperationValue))
        {
            return true;
        }

        return type.IsGenericType && type.GetGenericArguments().Any(ContainsBareCommandOperationCollection);
    }

    /// <summary>
    /// Get the best type from a tuple by selecting either a ConceptAs implementation or a primitive type.
    /// </summary>
    /// <param name="type">Tuple <see cref="Type"/> to inspect.</param>
    /// <returns>The best type found, the original non-tuple type, or null when every tuple value is server-handled.</returns>
    public static Type? GetBestTupleType(this Type type)
    {
        if (!type.IsGenericType || !(type.FullName?.StartsWith("System.ValueTuple") ?? false))
        {
            return type;
        }

        var genericArguments = type.GetGenericArguments()
            .Select(UnwrapType)
            .Where(_ => _?.IsServerHandledCommandResponseValue() == false)
            .Cast<Type>()
            .ToArray();
        if (genericArguments.Length == 0)
        {
            return null;
        }

        var conceptType = genericArguments.FirstOrDefault(t => t.IsConcept());
        if (conceptType is not null)
        {
            return conceptType;
        }

        var primitiveType = genericArguments.FirstOrDefault(t => t.IsAPrimitiveType());
        return primitiveType ?? genericArguments[0];
    }

    /// <summary>
    /// Get the best response type from a OneOf type by examining all alternatives.
    /// If an alternative is a tuple, applies tuple type extraction logic.
    /// </summary>
    /// <param name="type">OneOf <see cref="Type"/> to inspect.</param>
    /// <returns>The best response type found, or null if no suitable type is found.</returns>
    public static Type? GetBestOneOfResponseType(this Type type)
    {
        if (!type.IsOneOf())
        {
            return null;
        }

        var genericArguments = type.GetGenericArguments();
        var candidateTypes = new List<Type>();

        foreach (var arg in genericArguments)
        {
            var unwrappedType = UnwrapType(arg);
            if (unwrappedType?.IsServerHandledCommandResponseValue() == false)
            {
                candidateTypes.Add(unwrappedType);
            }
        }

        var conceptType = candidateTypes.Find(t => t.IsConcept());
        if (conceptType is not null)
        {
            return conceptType;
        }

        var primitiveType = candidateTypes.Find(t => t.IsAPrimitiveType());
        return primitiveType ?? candidateTypes.FirstOrDefault();
    }

    /// <summary>
    /// Find the identities of every response value type declared as consumed on the server.
    /// </summary>
    /// <param name="assemblies">Assemblies to inspect.</param>
    /// <param name="commandResponseValueHandlerContractsAssembly">The assembly defining the genuine handler contracts.</param>
    /// <param name="errorMessage">Callback for outputting error messages.</param>
    /// <returns>The assembly qualified names of the declared types.</returns>
    /// <remarks>
    /// Only assemblies that reference the contracts assembly are inspected. Declaring a handler requires implementing
    /// its contracts, so an assembly without that reference cannot contribute a declaration, and skipping it avoids
    /// materializing the type metadata of every unrelated dependency.
    /// </remarks>
    internal static HashSet<string> FindServerHandledCommandResponseValueTypeNames(
        IEnumerable<Assembly> assemblies,
        Assembly? commandResponseValueHandlerContractsAssembly,
        Action<string> errorMessage) =>
        commandResponseValueHandlerContractsAssembly is null
            ? []
            : assemblies
                .Where(CanDeclareCommandResponseValueHandlers)
                .SelectMany(_ => GetLoadableTypes(_, errorMessage))
                .Where(_ => !_.IsAbstract && !_.IsInterface)
                .Select(_ => GetLoadableInterfaces(_, errorMessage))
                .SelectMany(_ => GetServerHandledCommandResponseValueTypeNames(_, commandResponseValueHandlerContractsAssembly))
                .ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// Check whether an assembly could possibly declare a command response value handler.
    /// </summary>
    /// <param name="assembly">Assembly to check.</param>
    /// <returns>True if the assembly defines or references the handler contracts, false if not.</returns>
    internal static bool CanDeclareCommandResponseValueHandlers(Assembly assembly) =>
        assembly.GetName().Name == CommandResponseValueHandlerContractsAssemblyName ||
        assembly.GetReferencedAssemblies().Any(_ => _.Name == CommandResponseValueHandlerContractsAssemblyName);

    /// <summary>
    /// Get the response value types a handler declares as consumed on the server.
    /// </summary>
    /// <param name="interfaces">The interfaces implemented by the candidate handler.</param>
    /// <param name="commandResponseValueHandlerContractsAssembly">The assembly defining the genuine handler contracts.</param>
    /// <returns>The assembly qualified names of the declared types.</returns>
    /// <remarks>
    /// Both contracts are matched on assembly identity rather than name, so a dependency shipping look-alike
    /// interfaces in the same namespace cannot claim a value and have its client response suppressed.
    /// </remarks>
    internal static IEnumerable<string> GetServerHandledCommandResponseValueTypeNames(
        Type[] interfaces,
        Assembly? commandResponseValueHandlerContractsAssembly)
    {
        if (commandResponseValueHandlerContractsAssembly is null ||
            !ImplementsRuntimeHandlerContract(interfaces, commandResponseValueHandlerContractsAssembly))
        {
            return [];
        }

        return interfaces
            .Where(_ => _.IsGenericType &&
                        _.GetGenericTypeDefinition().FullName == CommandResponseValueHandlerTypeName &&
                        ReferenceEquals(_.Assembly, commandResponseValueHandlerContractsAssembly))
            .Select(_ => _.GetGenericArguments()[0].AssemblyQualifiedName)
            .Where(_ => _ is not null)
            .Cast<string>();
    }

    static bool ImplementsRuntimeHandlerContract(Type[] interfaces, Assembly contractsAssembly) =>
        interfaces.Any(_ => _.FullName == RuntimeCommandResponseValueHandlerTypeName &&
                            ReferenceEquals(_.Assembly, contractsAssembly));

    static bool IsCommandOperationValue(Type type)
    {
        var underlying = IsGenericTypeDefinition(type, typeof(Nullable<>)) ? type.GetGenericArguments()[0] : type;

        return EnumerateTypeAndContracts(underlying).Any(IsCommandOperationContract);
    }

    static bool IsCommandOperationContract(Type type)
    {
        if (type.FullName is not ("Cratis.Arc.Commands.ICommandOperation" or "Cratis.Arc.Commands.CommandOperations"))
        {
            return false;
        }

        var metadataAssemblies = _metadataLoadContext?.GetAssemblies().ToArray();
        var assemblies = metadataAssemblies?.Contains(type.Assembly) == true
            ? metadataAssemblies
            : AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic);
        var contracts = assemblies.FirstOrDefault(assembly => assembly.GetName().Name == CommandResponseValueHandlerContractsAssemblyName);

        return ReferenceEquals(type.Assembly, contracts);
    }

    /// <summary>
    /// Unwrap a type to get the best response type candidate.
    /// Handles tuples and OneOf types recursively.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to unwrap.</param>
    /// <returns>The unwrapped type, or null if the type should be skipped.</returns>
    static Type? UnwrapType(Type type)
    {
        if (type.IsServerHandledCommandResponseValue())
        {
            return null;
        }

        if (type.IsGenericType && (type.FullName?.StartsWith("System.ValueTuple") ?? false))
        {
            return type.GetBestTupleType();
        }

        if (type.IsOneOf())
        {
            return type.GetBestOneOfResponseType();
        }

        return type;
    }

    /// <summary>
    /// Find the declared server-handled response value identities from the assemblies loaded into the runtime.
    /// </summary>
    /// <returns>The assembly qualified names of the declared types.</returns>
    /// <remarks>
    /// Used for types outside the current metadata graph. More than one copy of the contracts assembly can be loaded
    /// when the generator is hosted rather than run as a tool; picking the first only narrows what is recognized as
    /// server-handled, which leaves the response generated rather than wrongly suppressed.
    /// </remarks>
    static HashSet<string> FindRuntimeServerHandledCommandResponseValueTypeNames()
    {
        var runtimeAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(_ => !_.IsDynamic).ToArray();
        return FindServerHandledCommandResponseValueTypeNames(
            runtimeAssemblies,
            Array.Find(runtimeAssemblies, _ => _.GetName().Name == CommandResponseValueHandlerContractsAssemblyName),
            _ => { });
    }

    static IEnumerable<Type> GetLoadableTypes(Assembly assembly, Action<string> errorMessage)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            foreach (var loaderException in ex.LoaderExceptions.Where(_ => _ is not null))
            {
                errorMessage($"Could not load a type from managed dependency '{assembly.FullName}': {loaderException!.Message}");
            }

            return ex.Types.Where(_ => _ is not null).Cast<Type>();
        }
        catch (Exception ex) when (ex is FileLoadException or FileNotFoundException)
        {
            errorMessage($"Could not inspect managed dependency '{assembly.FullName}': {ex.Message}");
            return [];
        }
    }

    static Type[] GetLoadableInterfaces(Type type, Action<string> errorMessage)
    {
        try
        {
            return type.GetInterfaces();
        }
        catch (Exception ex) when (ex is FileLoadException or FileNotFoundException or TypeLoadException)
        {
            errorMessage($"Could not inspect managed type '{type.FullName}': {ex.Message}");
            return [];
        }
    }

    static bool IsEnumerableOfCommandOperationValue(Type contract) =>
        contract.IsGenericType &&
        contract.GetGenericTypeDefinition().FullName == "System.Collections.Generic.IEnumerable`1" &&
        IsCommandOperationValue(contract.GetGenericArguments()[0]);
}
