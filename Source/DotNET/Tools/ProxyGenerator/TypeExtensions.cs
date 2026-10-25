// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Dynamic;
using System.Reflection;
using System.Runtime.Loader;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Extension methods for working with types.
/// </summary>
public static partial class TypeExtensions
{
    /// <summary>
    /// Gets the definition of object type that is a final one.
    /// </summary>
    public static readonly TargetType ObjectTypeFinal = new(typeof(object), "Record<string, unknown>", "Object", Final: true);

#pragma warning disable SA1600 // Elements should be documented
    internal static Type _conceptType = typeof(NoConcept);
    internal static Type _controllerBaseType = typeof(object);
    internal static Type _taskType = typeof(Task);
    internal static Type _voidType = typeof(void);
#pragma warning restore SA1600 // Elements should be documented

    static readonly TargetType _numberTargetType = new(typeof(int), "number", "Number");
    static readonly TargetType _dateTargetType = new(typeof(DateTimeOffset), "Date", "Date");
    static readonly TargetType _stringTargetType = new(typeof(string), "string", "String");
    static readonly TargetType _booleanTargetType = new(typeof(bool), "boolean", "Boolean");

    static readonly Dictionary<string, TargetType> _primitiveTypeMap = new()
    {
        { typeof(object).FullName!, ObjectTypeFinal },
        { typeof(char).FullName!, _stringTargetType },
        { typeof(byte).FullName!, _numberTargetType },
        { typeof(sbyte).FullName!, _numberTargetType },
        { typeof(bool).FullName!, _booleanTargetType },
        { typeof(string).FullName!, _stringTargetType },
        { typeof(short).FullName!, _numberTargetType },
        { typeof(int).FullName!, _numberTargetType },
        { typeof(long).FullName!, _numberTargetType },
        { typeof(ushort).FullName!, _numberTargetType },
        { typeof(uint).FullName!, _numberTargetType },
        { typeof(ulong).FullName!, _numberTargetType },
        { typeof(float).FullName!, _numberTargetType },
        { typeof(double).FullName!, _numberTargetType },
        { typeof(decimal).FullName!, _numberTargetType },
        { typeof(DateTime).FullName!, _dateTargetType },
        { typeof(DateTimeOffset).FullName!, _dateTargetType },
        { typeof(Guid).FullName!, new(typeof(Guid), "Guid", "Guid", "@cratis/fundamentals", FromPackage: true) },
        { typeof(TimeSpan).FullName!, new(typeof(TimeSpan), "TimeSpan", "TimeSpan", "@cratis/fundamentals", FromPackage: true) },

        // A calendar date and a time of day are not instants, and a JavaScript Date can only be one. Mapping them
        // onto Date invents an instant that was never sent - `new Date("2026-05-12")` is UTC midnight, which every
        // browser-local getter west of UTC reads back as the 11th, and `new Date("14:30:45")` is not a date at all
        // and yields Invalid Date. Both already cross the wire as their ISO-8601 string, so carrying that string
        // through is the faithful mapping: nothing is invented, nothing is lost, and the timezone decision moves to
        // the call site that actually needs one instead of being made silently by the deserializer.
        //
        // They map to first-class types rather than to string for the same reason Guid and TimeSpan do two lines
        // above: a string would be correct but say nothing about what it holds, leaving a calendar date
        // indistinguishable from any other string. Both carry converters in @cratis/fundamentals, so the value
        // arrives as the type rather than as text a call site has to parse.
        { typeof(DateOnly).FullName!, new(typeof(DateOnly), "DateOnly", "DateOnly", "@cratis/fundamentals", FromPackage: true) },
        { typeof(TimeOnly).FullName!, new(typeof(TimeOnly), "TimeOnly", "TimeOnly", "@cratis/fundamentals", FromPackage: true) },
        { typeof(System.Text.Json.Nodes.JsonNode).FullName!, ObjectTypeFinal },
        { typeof(System.Text.Json.Nodes.JsonObject).FullName!, ObjectTypeFinal },
        { typeof(System.Text.Json.Nodes.JsonArray).FullName!, ObjectTypeFinal },
        { typeof(System.Text.Json.JsonDocument).FullName!, ObjectTypeFinal },
        { typeof(Uri).FullName!, _stringTargetType },
        { "Cratis.Geospatial.Point", new(typeof(object), "Point", "Point", "@cratis/fundamentals", FromPackage: true) },
        { "Cratis.Geospatial.LineString", new(typeof(object), "LineString", "LineString", "@cratis/fundamentals", FromPackage: true) },
        { "Cratis.Geospatial.Polygon", new(typeof(object), "Polygon", "Polygon", "@cratis/fundamentals", FromPackage: true) }
    };

    /// <summary>
    /// Scalar types convertible as individual query arguments (unlike object and geospatial DTOs in the type-shape map).
    /// Mirrored by ConverterExtensions._additionalQueryArgumentScalarTypes and checked by the parity spec.
    /// </summary>
    static readonly HashSet<string> _queryArgumentScalarTypeNames =
    [
        typeof(string).FullName!,
        typeof(decimal).FullName!,
        typeof(DateTime).FullName!,
        typeof(DateTimeOffset).FullName!,
        typeof(TimeSpan).FullName!,
        typeof(Guid).FullName!,
        typeof(DateOnly).FullName!,
        typeof(TimeOnly).FullName!,
        typeof(Uri).FullName!,
        typeof(System.Text.Json.Nodes.JsonNode).FullName!,
        typeof(System.Text.Json.Nodes.JsonObject).FullName!,
        typeof(System.Text.Json.Nodes.JsonArray).FullName!,
        typeof(System.Text.Json.JsonDocument).FullName!
    ];

    static readonly Dictionary<string, Assembly> _assembliesByName = [];
    static readonly object _derivativeCandidatesLock = new();

    static Dictionary<string, string> _assemblyPackageMappings = [];
    static Dictionary<string, TargetType> _typeMappings = [];
    static HashSet<string> _excludedTypeNames = [];
    static List<string> _excludedNamespacePatterns = [];
    static List<(string Namespace, string Folder)> _namespaceRoots = [];

    static (IEnumerable<Assembly> Source, List<Type> Candidates)? _derivativeCandidates;

    static CanonicalAssemblyResolver? _assemblyResolver;
    static MetadataLoadContext? _metadataLoadContext;
    static Func<AssemblyLoadContext, AssemblyName, Assembly?>? _assemblyResolvingHandler;
    static HashSet<string>? _serverHandledCommandResponseValueTypeNames;
    static HashSet<string>? _serverHandledCommandResponseValueAttributeTypeNames;
    static HashSet<string>? _serverHandledCommandResponseValueEnumerableAttributeTypeNames;

    /// <summary>
    /// Gets all assemblies gathered from the <see cref="InitializeProjectAssemblies(string, Action{string}, Action{string})"/> method.
    /// </summary>
    public static IEnumerable<Assembly> Assemblies { get; private set; } = [];

    /// <summary>
    /// Gets the managed application dependencies (projects and packages) that can declare runtime validators.
    /// </summary>
    internal static IEnumerable<Assembly> ValidatorAssemblies { get; private set; } = [];

    /// <summary>
    /// Sets the assembly-to-package mappings used to map types from specific assemblies to external TypeScript packages.
    /// </summary>
    /// <param name="mappings">Dictionary mapping assembly names to package names.</param>
    public static void SetAssemblyPackageMappings(IReadOnlyDictionary<string, string> mappings)
    {
        _assemblyPackageMappings = new Dictionary<string, string>(mappings);
    }

    /// <summary>
    /// Sets explicit mappings from a .NET type to the TypeScript type it should cross the wire as.
    /// </summary>
    /// <param name="mappings">Triples of (fully qualified type name, TypeScript type, package the type is imported from).</param>
    /// <remarks>
    /// <para>
    /// Consulted ahead of the built-in map, so a consumer can correct how an existing type is generated
    /// as well as declare one the generator has never seen. Nothing is mapped unless asked for, so a
    /// build that passes no mappings generates exactly what it generated before.
    /// </para>
    /// <para>
    /// A mapping with no package generates a bare TypeScript type and emits no import; one with a
    /// package emits the package as a bare specifier, the way <c>Guid</c> and <c>TimeSpan</c> are
    /// imported from <c>@cratis/fundamentals</c>.
    /// </para>
    /// </remarks>
    public static void SetTypeMappings(IReadOnlyCollection<(string TypeName, string TsType, string Package)> mappings)
    {
        _typeMappings = mappings.ToDictionary(
            static mapping => mapping.TypeName,
            static mapping => new TargetType(
                typeof(object),
                mapping.TsType,
                mapping.TsType,
                mapping.Package,
                Final: true,
                FromPackage: !string.IsNullOrEmpty(mapping.Package)),
            StringComparer.Ordinal);
    }

    /// <summary>
    /// Sets the types and namespace patterns to exclude from proxy generation.
    /// </summary>
    /// <param name="typeNames">Fully qualified type names to exclude.</param>
    /// <param name="namespacePatterns">Namespace glob patterns (supports <c>*</c> as wildcard) to exclude.</param>
    public static void SetExcludedTypes(IReadOnlyCollection<string> typeNames, IReadOnlyCollection<string> namespacePatterns)
    {
        _excludedTypeNames = [.. typeNames];
        _excludedNamespacePatterns = [.. namespacePatterns];
    }

    /// <summary>
    /// Sets the namespace roots used to determine the output folder structure.
    /// When a type's namespace begins with a configured root, that root prefix is stripped
    /// and the remainder is appended under the specified base folder.
    /// </summary>
    /// <param name="roots">Pairs of (namespace, base folder), e.g. <c>("MyApp.Features", "features")</c>.</param>
    public static void SetNamespaceRoots(IReadOnlyCollection<(string Namespace, string Folder)> roots)
    {
        _namespaceRoots = [.. roots];
    }

    /// <summary>
    /// Check whether a type is excluded from proxy generation by an exact type name match or a namespace glob pattern.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns><see langword="true"/> if the type is excluded; otherwise <see langword="false"/>.</returns>
    public static bool IsExcluded(this Type type)
    {
        if (type.FullName is null) return false;
        if (_excludedTypeNames.Contains(type.FullName)) return true;
        var ns = type.Namespace ?? string.Empty;
        return _excludedNamespacePatterns.Exists(pattern => MatchesGlobPattern(ns, pattern));
    }

    /// <summary>
    /// Check if a type is from a mapped assembly and should be imported from a configured package rather than generated locally.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if the type is from a mapped assembly, false otherwise.</returns>
    public static bool IsFromMappedAssembly(this Type type) =>
        type.Assembly.GetName().Name is { } name && _assemblyPackageMappings.ContainsKey(name);

    /// <summary>
    /// Check if a type is a flags enum. Uses <see cref="CustomAttributeData"/> to support types loaded via <see cref="MetadataLoadContext"/>.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if the type has the <see cref="FlagsAttribute"/>, false otherwise.</returns>
    public static bool IsFlagsEnum(this Type type) =>
        type.GetCustomAttributesData().Any(attr => attr.AttributeType.FullName == typeof(FlagsAttribute).FullName);

    /// <summary>
    /// Initialize the project assemblies.
    /// </summary>
    /// <param name="assemblyFile">Assembly file to start from.</param>
    /// <param name="message">Callback for outputting messages.</param>
    /// <param name="errorMessage">Callback for outputting error messages.</param>
    /// <returns>True if successful, false if not.</returns>
    public static bool InitializeProjectAssemblies(string assemblyFile, Action<string> message, Action<string> errorMessage) =>
        TryInitializeProjectAssemblies(assemblyFile, message, errorMessage);

    /// <summary>
    /// Check if a type is a controller.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns>True if it is a controller, false if not.</returns>
    public static bool IsController(this Type type)
    {
        try
        {
            return type.IsAssignableTo(_controllerBaseType);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if a type is an async enumerable.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if it is an async enumerable, false if not.</returns>
    public static bool IsAsyncEnumerable(this Type type) =>
        EnumerateTypeAndContracts(type).Any(_ => IsGenericTypeDefinition(_, typeof(IAsyncEnumerable<>)));

    /// <summary>
    /// Check if a type is observable.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if it is observable, false if not.</returns>
    public static bool IsSubject(this Type type) =>
        (type.FullName?.StartsWith("System.Reactive.Subjects.ISubject`1") ?? false) ||
        (type.FullName?.StartsWith("System.IObservable`1") ?? false);

    /// <summary>
    /// Check if a type is a task or a task of T.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if it is a task, false if not.</returns>
    public static bool IsTask(this Type type) =>
        (type.FullName?.StartsWith(_taskType.FullName!) ?? false) ||
        (type.IsGenericType && (type.GetGenericTypeDefinition().FullName?.StartsWith(typeof(Task<>).FullName!) ?? false));

    /// <summary>
    /// Check if a type is a String or not.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns>True if type is a string, false otherwise.</returns>
    public static bool IsString(this Type type) => type.FullName == typeof(string).FullName;

    /// <summary>
    /// Check if a type is assignable to a specific type.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <typeparam name="T">Type to check if is assignable to.</typeparam>
    /// <returns>True if it is, false if not.</returns>
    public static bool IsAssignableTo<T>(this Type type)
    {
        if (_assembliesByName.TryGetValue(typeof(T).Assembly.GetName().Name!, out var assembly))
        {
            return type.IsAssignableTo(assembly.GetType(typeof(T).FullName!));
        }
        return false;
    }

    /// <summary>
    /// Check whether or not a <see cref="Type"/> is a known type in TypeScript.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns>True if it is known, false if not.</returns>
    public static bool IsKnownType(this Type type)
    {
        if (type.IsDictionary())
        {
            return true;
        }

        if (type.IsConcept())
        {
            type = type.GetConceptValueType();
        }

        // Unwrap nullable types before checking the primitive map - Nullable<T> primitives
        // should be treated as known types since they map to TypeScript primitives
        if (IsGenericTypeDefinition(type, typeof(Nullable<>)))
        {
            type = type.GetGenericArguments()[0];
        }

        return type.FullName is not null && _primitiveTypeMap.ContainsKey(type.FullName);
    }

    /// <summary>
    /// Check if a type needs an import statement.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if it has, false if not.</returns>
    public static bool HasModule(this Type type)
    {
        var targetType = type.GetTargetType();
        return !string.IsNullOrEmpty(targetType.Module);
    }

    /// <summary>
    /// Check if a type is a dictionary.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if it is, false if not.</returns>
    public static bool IsDictionary(this Type type) => type.GetDictionaryInterface() is not null;

    /// <summary>
    /// Get the dictionary interface a <see cref="Type"/> is, or implements - mutable or read-only.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get it for.</param>
    /// <returns>The closed dictionary interface, or null when the type is not a dictionary.</returns>
    /// <remarks>
    /// Both shapes count. <see cref="IReadOnlyDictionary{TKey, TValue}"/> does not implement
    /// <see cref="IDictionary{TKey, TValue}"/> - they are siblings - so a model that exposes its
    /// dictionaries read-only, which is the natural shape for an immutable record, would otherwise not be
    /// recognized as holding dictionaries at all. It would then fall through to the enumerable path and
    /// come out as an array of <see cref="KeyValuePair{TKey, TValue}"/> rather than a keyed object.
    /// </remarks>
    public static Type? GetDictionaryInterface(this Type type)
    {
        if (IsDictionaryDefinition(type))
        {
            return type;
        }

        foreach (var candidate in type.GetInterfaces())
        {
            if (!candidate.IsGenericType) continue;
            if (IsDictionaryDefinition(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Get the key type of a dictionary type.
    /// </summary>
    /// <param name="type">Dictionary type to get key type from.</param>
    /// <returns>The key <see cref="Type"/>.</returns>
    public static Type GetDictionaryKeyType(this Type type)
    {
        var dictionaryInterface = type.GetDictionaryInterface()!;
        return dictionaryInterface.GetGenericArguments()[0];
    }

    /// <summary>
    /// Get the value type of a dictionary type.
    /// </summary>
    /// <param name="type">Dictionary type to get value type from.</param>
    /// <returns>The value <see cref="Type"/>.</returns>
    public static Type GetDictionaryValueType(this Type type)
    {
        var dictionaryInterface = type.GetDictionaryInterface()!;
        return dictionaryInterface.GetGenericArguments()[1];
    }

    /// <summary>
    /// Get the TypeScript type string for the value type of a dictionary, handling collection value types
    /// by converting them to TypeScript array syntax (e.g., <c>IList&lt;T&gt;</c> becomes <c>T[]</c>).
    /// Non-collection, non-dictionary value types are resolved directly via <see cref="GetTargetType"/>.
    /// </summary>
    /// <param name="valueType">The dictionary value type.</param>
    /// <returns>
    /// The TypeScript type string: <c>T[]</c> when the value type is an enumerable collection,
    /// or the plain target type name when the value type is a scalar.
    /// </returns>
    public static string GetDictionaryValueTypeString(this Type valueType)
    {
        var targetType = valueType.GetDictionaryEffectiveValueType().GetTargetType().Type;
        if (valueType.IsEnumerable() && !valueType.IsDictionary())
        {
            return valueType.GetEnumerableElementType().IsNullable() ? $"({targetType} | null)[]" : $"{targetType}[]";
        }

        return valueType.IsNullable() ? $"{targetType} | null" : targetType;
    }

    /// <summary>
    /// Resolve the effective element type for the value of a dictionary, unwrapping one level of collection
    /// when the value type is an enumerable (e.g., <c>IList&lt;T&gt;</c> → <c>T</c>).
    /// Nullable value types and concepts are resolved to their underlying types.
    /// </summary>
    /// <param name="valueType">The dictionary value type.</param>
    /// <returns>The effective element type to use for proxy generation and type collection.</returns>
    public static Type GetDictionaryEffectiveValueType(this Type valueType)
    {
        var effectiveType = valueType.IsEnumerable() && !valueType.IsDictionary()
            ? valueType.GetEnumerableElementType() ?? valueType
            : valueType;
        if (effectiveType.IsNullable())
        {
            effectiveType = effectiveType.GetGenericArguments()[0];
        }

        return effectiveType.IsConcept() ? effectiveType.GetConceptValueType() : effectiveType;
    }

    /// <summary>
    /// Check if a type is a System type (i.e., its namespace is "System" or starts with "System.").
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if it is a System type, false if not.</returns>
    public static bool IsSystemType(this Type type) =>
        type.Namespace is not null &&
        (type.Namespace == "System" || type.Namespace.StartsWith("System.", StringComparison.Ordinal));

    /// <summary>
    /// Check whether a type is a valid user-defined base type for TypeScript inheritance.
    /// Returns <see langword="true"/> when the base type should produce an <c>extends</c> clause
    /// in the generated TypeScript class. System.* types, primitives, excluded types, and types
    /// from mapped assemblies are all excluded.
    /// </summary>
    /// <param name="type">The candidate base type to evaluate.</param>
    /// <returns>True if the type should be used as a TypeScript base class, false otherwise.</returns>
    public static bool IsValidInheritanceBaseType(this Type type) =>
        !type.IsAPrimitiveType() && !type.IsSystemType() && !type.IsExcluded() && !type.IsFromMappedAssembly();

    /// <summary>
    /// Check if a type is enumerable. Note that string is an IEnumerable, but in this case the string is excluded, as well as ExpandoObject.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns>True if type is enumerable, false if not an enumerable.</returns>
    public static bool IsEnumerable(this Type type)
    {
        return !type.IsAPrimitiveType() &&
               type.FullName != typeof(ExpandoObject).FullName &&
               !type.IsString() &&
               EnumerateTypeAndContracts(type).Any(_ => _.FullName == typeof(IEnumerable).FullName);
    }

    /// <summary>
    /// Check if a type is an enumerable of primitive, concept, or enum types.
    /// Supports <see cref="IEnumerable{T}"/>, arrays and other collection types where the element type is primitive, concept, or an enum.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns>True if type is an enumerable of primitives, concepts, or enums, false otherwise.</returns>
    /// <remarks>
    /// Mirrored by <c>ConverterExtensions.IsEnumerableOfQueryArgumentElement</c> in Arc.Core
    /// (Source/DotNET/Arc.Core/ConverterExtensions.cs) - the runtime uses that predicate to decide the same
    /// question (is this parameter a caller-supplied query argument or an injected dependency) against real loaded
    /// types rather than the <c>MetadataLoadContext</c> types this generator works with, so the two cannot share
    /// source. Both must agree on which shapes qualify; see <c>for_TypeExtensions/when_checking_is_enumerable_of_primitive_or_concept</c>
    /// here and <c>for_ModelBoundQueryPerformer/when_getting_parameters</c> in Arc.Core.Specs for the specs that pin
    /// them together.
    /// </remarks>
    public static bool IsEnumerableOfPrimitiveOrConcept(this Type type)
    {
        if (!type.IsEnumerable()) return false;

        var elementType = type.GetEnumerableElementType();
        if (elementType is null) return false;

        var scalarType = elementType.IsNullable() ? elementType.GetGenericArguments()[0] : elementType;
        return scalarType.IsConcept() || scalarType.IsEnum || scalarType.GetTypeInfo().IsPrimitive ||
            (scalarType.FullName is not null && _queryArgumentScalarTypeNames.Contains(scalarType.FullName));
    }

    /// <summary>
    /// Check if a type is a string-keyed query argument dictionary with scalar values.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="valueType">The dictionary value type.</param>
    /// <returns>True if the dictionary has convertible scalar values.</returns>
    public static bool IsDictionaryOfQueryArgumentValues(this Type type, out Type valueType)
    {
        valueType = typeof(object);
        var contract = type.GetDictionaryInterface();
        if (contract is null || contract.GetGenericArguments()[0].FullName != typeof(string).FullName) return false;
        valueType = contract.GetGenericArguments()[1];
        var scalarType = valueType.IsNullable() ? valueType.GetGenericArguments()[0] : valueType;
        return scalarType.IsConcept() || scalarType.IsEnum || scalarType.GetTypeInfo().IsPrimitive ||
            (scalarType.FullName is not null && _queryArgumentScalarTypeNames.Contains(scalarType.FullName));
    }

    /// <summary>
    /// Check if a type is a "primitive" type.  This is not just dot net primitives but basic types like string, decimal, datetime,
    /// that are not classified as primitive types.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns>True if <see cref="Type"/> is a primitive type.</returns>
    public static bool IsAPrimitiveType(this Type type)
    {
        return type.GetTypeInfo().IsPrimitive
                || type.IsNullable() || (type.FullName is not null && _primitiveTypeMap.ContainsKey(type.FullName));
    }

    /// <summary>
    /// Check if a type is nullable or not.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns>True if type is nullable, false if not.</returns>
    public static bool IsNullable(this Type type)
    {
        while (!type.Equals(typeof(object)))
        {
            if (IsGenericTypeDefinition(type, typeof(Nullable<>)))
            {
                return true;
            }

            if (type.BaseType is null) break;

            type = type.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Gets the element type of an enumerable.
    /// </summary>
    /// <param name="enumerableType">The <see cref="Type">type of the enumerable</see>.</param>
    /// <returns>Enumerable element <see cref="Type"/>.</returns>
    /// <remarks>
    /// https://stackoverflow.com/questions/906499/getting-type-t-from-ienumerablet.
    /// </remarks>
    public static Type GetEnumerableElementType(this Type enumerableType)
    {
        if (enumerableType.IsArray)
        {
            return enumerableType.GetElementType()!;
        }

        if (IsGenericTypeDefinition(enumerableType, typeof(IEnumerable<>)))
        {
            return enumerableType.GetGenericArguments()[0];
        }

        return enumerableType.GetInterfaces()
            .Where(_ => IsGenericTypeDefinition(_, typeof(IEnumerable<>)))
            .Select(t => t.GenericTypeArguments[0]).FirstOrDefault()!;
    }

    /// <summary>
    /// Gets the element type of a System.Reactive.Subjects.ISubject{T}.
    /// </summary>
    /// <param name="subjectType">The <see cref="Type"/> to get from.</param>
    /// <returns>The element type.</returns>
    public static Type GetSubjectElementType(this Type subjectType)
    {
        if (subjectType.IsGenericType && subjectType.IsSubject())
        {
            return subjectType.GetGenericArguments()[0];
        }

        return subjectType.GetInterfaces()
            .Where(t => t.IsGenericType &&
                t.GetGenericTypeDefinition().Name.StartsWith("ISubject`1"))
            .Select(t => t.GenericTypeArguments[0]).FirstOrDefault()!;
    }

    /// <summary>
    /// Gets the element type of an <see cref="IAsyncEnumerable{T}"/>.
    /// </summary>
    /// <param name="asyncEnumerableType">The <see cref="Type"/> to get from.</param>
    /// <returns>The element type.</returns>
    public static Type GetAsyncEnumerableElementType(this Type asyncEnumerableType)
    {
        if (IsGenericTypeDefinition(asyncEnumerableType, typeof(IAsyncEnumerable<>)))
        {
            return asyncEnumerableType.GetGenericArguments()[0];
        }

        return asyncEnumerableType.GetInterfaces()
            .Where(_ => IsGenericTypeDefinition(_, typeof(IAsyncEnumerable<>)))
            .Select(t => t.GenericTypeArguments[0]).FirstOrDefault()!;
    }

    /// <summary>
    /// Check if a type derives from an open generic type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <param name="openGenericType">Open generic <see cref="Type"/> to check for.</param>
    /// <returns>True if type matches the open generic <see cref="Type"/>.</returns>
    public static bool IsDerivedFromOpenGeneric(this Type type, Type openGenericType)
    {
        var typeToCheck = type;
        while (typeToCheck != null && typeToCheck != typeof(object))
        {
            var currentType = typeToCheck.GetTypeInfo().IsGenericType ? typeToCheck.GetGenericTypeDefinition() : typeToCheck;
            if (openGenericType == currentType)
                return true;

            typeToCheck = typeToCheck.GetTypeInfo().BaseType;
        }

        return false;
    }

    /// <summary>
    /// Check if a type is a concept or not.
    /// </summary>
    /// <param name="objectType"><see cref="Type"/> to check.</param>
    /// <returns>True if type is a concept, false if not.</returns>
    public static bool IsConcept(this Type objectType)
    {
        // First try direct type comparison (works when using MetadataLoadContext)
        if (objectType.IsDerivedFromOpenGeneric(_conceptType))
        {
            return true;
        }

        // Fallback: Check by type name for runtime types when MetadataLoadContext isn't initialized
        // This handles cases where tests use runtime types directly without Initialize()
        var typeToCheck = objectType;
        while (typeToCheck != null && typeToCheck != typeof(object))
        {
            if (typeToCheck.IsGenericType)
            {
                var genericTypeDef = typeToCheck.GetGenericTypeDefinition();
                if (genericTypeDef.FullName == "Cratis.Concepts.ConceptAs`1")
                {
                    return true;
                }
            }

            typeToCheck = typeToCheck.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Get the type of the value inside a concept.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get value type from.</param>
    /// <returns>The type of the concept value.</returns>
    public static Type GetConceptValueType(this Type type)
    {
        return ConceptMap.GetConceptValueType(type);
    }

    /// <summary>
    /// Check if a type implements an open generic type.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <param name="openGenericType">Open generic <see cref="Type"/> to check for.</param>
    /// <returns>True if type implements the open generic <see cref="Type"/>.</returns>
    public static bool ImplementsOpenGeneric(this Type type, Type openGenericType)
    {
        var openGenericTypeInfo = openGenericType.GetTypeInfo();
        var typeInfo = type.GetTypeInfo();

        return typeInfo.GetInterfaces()
            .Any(i => i.GetTypeInfo().IsGenericType && i.GetTypeInfo().GetGenericTypeDefinition().GetTypeInfo() == openGenericTypeInfo);
    }

    /// <summary>
    /// Check if a type implements IEnumerable.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns>True if type implements IEnumerable, false if not.</returns>
    public static bool ImplementsEnumerable(this Type type) =>
        EnumerateTypeAndContracts(type).Any(_ => IsGenericTypeDefinition(_, typeof(IEnumerable<>)));

    /// <summary>
    /// Check if a type implements IOneOf from the OneOf namespace.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns>True if type implements IOneOf, false if not.</returns>
    public static bool IsOneOf(this Type type)
    {
        return type.GetInterfaces().Any(i =>
            i.FullName == "OneOf.IOneOf" ||
            (i.Namespace == "OneOf" && i.Name == "IOneOf"));
    }

    /// <summary>
    /// Get the derived type identifier for a type if it has a <c>DerivedTypeAttribute</c>.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to check.</param>
    /// <returns>The derived type identifier string, or null if the type is not decorated.</returns>
    public static string? GetDerivedTypeId(this Type type)
    {
        var attr = type.GetCustomAttributesData()
            .FirstOrDefault(a => a.AttributeType.FullName == "Cratis.Serialization.DerivedTypeAttribute");

        if (attr is null) return null;

        return attr.ConstructorArguments.Count > 0
            ? attr.ConstructorArguments[0].Value?.ToString()
            : null;
    }

    /// <summary>
    /// Get all non-abstract types that derive from or implement the given type and carry a <c>DerivedTypeAttribute</c>.
    /// </summary>
    /// <param name="baseType">Base type to find derivatives of.</param>
    /// <returns>Collection of derivative types.</returns>
    public static IEnumerable<Type> GetDerivativeTypes(this Type baseType)
    {
        if (CannotHaveDerivatives(baseType))
        {
            return [];
        }

        var assemblies = Assemblies;
        var candidates = assemblies.Any()
            ? DerivativeCandidatesOf(assemblies)
            : AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(GetTypesIgnoringLoadFailures)
                .Where(IsDerivativeCandidate)
                .ToList();

        return candidates.Where(t => IsDerivativeOf(t, baseType));
    }

    /// <summary>
    /// Gets the types of the given assemblies that can be a derivative of something, reading them once per set of assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to search.</param>
    /// <returns>The candidate types, in assembly and declaration order.</returns>
    /// <remarks>
    /// Finding the derivatives of a type is asked for every type the generator collects. Walking every type of every
    /// assembly, and reading its attributes, each time makes that quadratic on a large solution, while only the few
    /// types carrying a derived type identifier can ever answer it.
    /// </remarks>
    static List<Type> DerivativeCandidatesOf(IEnumerable<Assembly> assemblies)
    {
        lock (_derivativeCandidatesLock)
        {
            if (_derivativeCandidates is { } cached && ReferenceEquals(cached.Source, assemblies))
            {
                return cached.Candidates;
            }

            var candidates = assemblies
                .SelectMany(GetTypesIgnoringLoadFailures)
                .Where(IsDerivativeCandidate)
                .ToList();
            _derivativeCandidates = (assemblies, candidates);

            return candidates;
        }
    }

    static bool IsDerivativeCandidate(Type type) =>
        !type.IsAbstract &&
        !type.IsInterface &&
        type.GetDerivedTypeId() is not null;

    static IEnumerable<Type> EnumerateTypeAndContracts(Type type)
    {
        yield return type;

        foreach (var contract in type.GetInterfaces())
        {
            yield return contract;
        }

        var baseType = type.BaseType;
        while (baseType is not null)
        {
            yield return baseType;
            baseType = baseType.BaseType;
        }
    }

    static bool IsDictionaryDefinition(Type type)
    {
        try
        {
            return IsGenericTypeDefinition(type, typeof(IDictionary<,>)) ||
                   IsGenericTypeDefinition(type, typeof(IReadOnlyDictionary<,>));
        }
        catch
        {
            // GetGenericTypeDefinition can throw on some types
            return false;
        }
    }

    static bool IsGenericTypeDefinition(Type type, Type runtimeGenericTypeDefinition) =>
        type.IsGenericType && type.GetGenericTypeDefinition().FullName == runtimeGenericTypeDefinition.FullName;

    static bool CannotHaveDerivatives(Type type) => type.IsAPrimitiveType() || type.IsConcept() || type.IsEnum;

    static IEnumerable<Type> GetTypesIgnoringLoadFailures(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t is not null).Cast<Type>();
        }
    }

    static bool IsDerivativeOf(Type type, Type baseType) =>
        type != baseType &&
        !type.IsAbstract &&
        !type.IsInterface &&
        type.GetDerivedTypeId() is not null &&
        type.IsAssignableToBaseType(baseType);

    /// <summary>
    /// Match a namespace string against a glob pattern where <c>*</c> matches any sequence of characters.
    /// </summary>
    /// <param name="ns">Namespace to test.</param>
    /// <param name="pattern">Glob pattern (may contain one or more <c>*</c> wildcards).</param>
    /// <returns><see langword="true"/> if the namespace matches the pattern.</returns>
    static bool MatchesGlobPattern(string ns, string pattern)
    {
        if (!pattern.Contains('*')) return ns == pattern;

        var parts = pattern.Split('*');
        var index = 0;
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.Length == 0) continue;

            var found = ns.IndexOf(part, index, StringComparison.Ordinal);
            if (!IsValidGlobSegmentMatch(found, i)) return false;

            index = found + part.Length;
        }

        // If the pattern does not end with *, the namespace must be fully consumed
        return pattern.EndsWith('*') || index == ns.Length;
    }

    /// <summary>
    /// Check whether a glob segment was found where it is allowed to be.
    /// The first segment must match at position 0 (pattern must start from the beginning unless leading *).
    /// </summary>
    /// <param name="foundIndex">Index the segment was found at, or a negative number when it was not found.</param>
    /// <param name="segmentIndex">Index of the segment within the pattern.</param>
    /// <returns><see langword="true"/> if the segment match is acceptable.</returns>
    static bool IsValidGlobSegmentMatch(int foundIndex, int segmentIndex) =>
        foundIndex >= 0 && (segmentIndex != 0 || foundIndex == 0);

    static bool IsAssignableToBaseType(this Type type, Type baseType)
    {
        var baseFullName = baseType.FullName;
        if (string.IsNullOrEmpty(baseFullName)) return false;

        if (type.GetInterfaces().Any(i => i.FullName == baseFullName)) return true;

        var current = type.BaseType;
        while (current is not null && current.FullName != typeof(object).FullName)
        {
            if (current.FullName == baseFullName) return true;
            current = current.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Type used when no ConceptAs from Fundamentals is available.
    /// </summary>
    sealed record NoConcept();
}
