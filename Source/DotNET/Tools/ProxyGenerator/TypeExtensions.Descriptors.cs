// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.RegularExpressions;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

public static partial class TypeExtensions
{
    /// <summary>
    /// Get property descriptors for a type, only including properties declared directly on the type.
    /// </summary>
    /// <param name="type">Type to get for.</param>
    /// <returns>Collection of <see cref="PropertyDescriptor"/>.</returns>
    public static IEnumerable<PropertyDescriptor> GetPropertyDescriptors(this Type type)
    {
        if (type.IsAPrimitiveType()) return [];

        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).ToList().ConvertAll(_ => _.ToPropertyDescriptor());
    }

    /// <summary>
    /// Get target type for a type.
    /// </summary>
    /// <param name="type">Type to get for.</param>
    /// <returns>The <see cref="TargetType"/>.</returns>
    public static TargetType GetTargetType(this Type type)
    {
        if (type.IsEnum)
        {
            return new(type, type.Name, "Number");
        }

        if (type.IsDictionary())
        {
            return GetDictionaryTargetType(type);
        }

        if (type.IsConcept())
        {
            type = type.GetConceptValueType();
        }

        // Handle nullable types (Nullable<T>) by unwrapping and getting the target type of the underlying type
        // IMPORTANT: Cannot use Nullable.GetUnderlyingType() with MetadataLoadContext types because it's a
        // runtime reflection API that accesses private CLR implementation details. MetadataLoadContext types
        // are metadata-only and not backed by runtime CLR types, so Nullable.GetUnderlyingType returns null.
        // Instead, we use GetGenericArguments()[0] which works with metadata-only reflection.
        if (IsGenericTypeDefinition(type, typeof(Nullable<>)))
        {
            var underlyingType = type.GetGenericArguments()[0];
            return underlyingType.GetTargetType();
        }

        if (TryGetKnownTargetType(type, out var known))
        {
            return known;
        }

        var typeName = StripGenericArity(type.Name);
        if (_assemblyPackageMappings.TryGetValue(type.Assembly.GetName().Name!, out var packageName))
        {
            return new TargetType(type, typeName, typeName, packageName, FromPackage: true);
        }

        return new TargetType(type, typeName, typeName);
    }

    /// <summary>
    /// Convert a <see cref="Type"/> to a <see cref="TypeDescriptor"/>.
    /// </summary>
    /// <param name="type">Type to convert.</param>
    /// <param name="targetPath">The target path the proxies are generated to.</param>
    /// <param name="segmentsToSkip">Number of segments to skip from the namespace when generating the output path.</param>
    /// <returns>Converted <see cref="TypeDescriptor"/>.</returns>
    public static TypeDescriptor ToTypeDescriptor(this Type type, string targetPath, int segmentsToSkip)
    {
        var typesInvolved = new List<Type>();

        var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).ToList();
        var propertyDescriptors = properties.ConvertAll(_ => _.ToPropertyDescriptor());
        List<ImportStatement> imports = [];

        foreach (var property in propertyDescriptors)
        {
            AddPropertyImports(property, typesInvolved, imports);
        }

        // Detect base type for TypeScript inheritance (skip system types and object)
        string? baseTypeName = null;
        if (type.BaseType is { } baseType && baseType.IsValidInheritanceBaseType())
        {
            baseTypeName = baseType.GetTargetType().Type;
            baseType.CollectTypesInvolved(typesInvolved);
        }

        var relativePath = type.ResolveTargetPath(segmentsToSkip);
        imports.AddRange(typesInvolved.GetImports(targetPath, relativePath, segmentsToSkip));
        imports.AddRange(GetDerivativeImports(propertyDescriptors, targetPath, relativePath, segmentsToSkip));

        var referencedImports = GetReferencedImports(imports.DistinctBy(_ => _.Type), propertyDescriptors, baseTypeName)
            .Where(_ => _.OriginalType != type && !_.OriginalType.IsAssignableToBaseType(type))
            .ToList();

        var documentation = type.GetDocumentation();
        var derivedTypeId = type.GetDerivedTypeId();

        return new TypeDescriptor(
            type,
            type.GetTargetType().Type,
            propertyDescriptors,
            referencedImports.ToOrderedImports(),
            typesInvolved,
            documentation,
            derivedTypeId,
            baseTypeName);
    }

    /// <summary>
    /// Convert a <see cref="Type"/> to a <see cref="ModelDescriptor"/>.
    /// </summary>
    /// <param name="type">Type to convert.</param>
    /// <returns>Converted <see cref="ModelDescriptor"/>.</returns>
    public static ModelDescriptor ToModelDescriptor(this Type type)
    {
        // Unwrap ActionResult<T> or ActionResult (for controller-based queries/commands)
        if (type.IsGenericType)
        {
            var genericTypeDefinition = type.GetGenericTypeDefinition();
            if (genericTypeDefinition.FullName == "Microsoft.AspNetCore.Mvc.ActionResult`1")
            {
                type = type.GetGenericArguments()[0];
            }
        }

        var isSubject = type.IsSubject();
        if (isSubject)
        {
            type = type.GetSubjectElementType();
        }
        var isAsyncEnumerable = type.IsAsyncEnumerable();
        if (isAsyncEnumerable)
        {
            type = type.GetAsyncEnumerableElementType();
        }

        var isEnumerable = type.IsEnumerable();
        if (isEnumerable)
        {
            type = type.GetEnumerableElementType();
        }

        var targetType = type.GetTargetType();

        return new(
            type,
            targetType.Type,
            targetType.Constructor,
            isEnumerable,
            isSubject || isAsyncEnumerable,
            Enumerable.Empty<ImportStatement>().ToOrderedImports());
    }

    /// <summary>
    /// Convert a <see cref="Type"/> to a <see cref="EnumDescriptor"/>.
    /// </summary>
    /// <param name="type">Enum type to convert.</param>
    /// <returns>Converted <see cref="EnumDescriptor"/>.</returns>
    public static EnumDescriptor ToEnumDescriptor(this Type type)
    {
        // Keep the underlying values boxed as-is. GetValuesAsUnderlyingType returns the enum's actual underlying
        // type (byte, long, ...), and Cast<int> unbox-casts each element to int, which throws for any enum not
        // backed by int — e.g. `enum X : byte` or `[Flags] enum X : long`. EnumMemberDescriptor.Value is object
        // and the flags math uses Convert.ToInt64, so the boxed underlying value flows through unchanged.
        var values = Enum.GetValuesAsUnderlyingType(type).Cast<object>();
        var names = Enum.GetNames(type);
        var members = values.Select((value, index) => new EnumMemberDescriptor(names[index], value)).ToArray();
        var isFlags = type.IsFlagsEnum();
        var documentation = type.GetDocumentation();
        return new EnumDescriptor(type, type.Name, members, [], isFlags, documentation);
    }

    /// <summary>
    /// Resolve the relative path for a type.
    /// Namespace roots (configured via <see cref="SetNamespaceRoots"/>) take priority over
    /// <paramref name="segmentsToSkip"/>: when the type's namespace begins with a root the root
    /// prefix is stripped and the remainder becomes the folder path.
    /// </summary>
    /// <param name="type">Type to resolve for.</param>
    /// <param name="segmentsToSkip">Number of segments to skip from the namespace when generating the output path.</param>
    /// <returns>Resolved path.</returns>
    public static string ResolveTargetPath(this Type type, int segmentsToSkip)
    {
        var ns = type.Namespace ?? string.Empty;

        var matchingRoot = _namespaceRoots
            .Where(root => ns == root.Namespace || ns.StartsWith(root.Namespace + ".", StringComparison.Ordinal))
            .OrderByDescending(r => r.Namespace.Length)
            .Cast<(string Namespace, string Folder)?>()
            .FirstOrDefault();

        if (matchingRoot is not null)
        {
            return ResolveNamespaceRootPath(ns, matchingRoot.Value);
        }

        var stripped = ns.Replace(Globals.NamespacePrefix, string.Empty);
        var path = string.Join(Path.DirectorySeparatorChar, stripped.Split('.').Skip(segmentsToSkip));
        if (string.IsNullOrEmpty(path))
        {
            path = $"{Path.DirectorySeparatorChar}";
        }

        return path;
    }

    /// <summary>
    /// Get imports from a collection of types.
    /// </summary>
    /// <param name="type">Type to get from.</param>
    /// <param name="targetPath">The target path the proxies are generated to.</param>
    /// <param name="relativePath">The relative path to work from.</param>
    /// <param name="segmentsToSkip">Number of segments to skip from the namespace when generating the output path.</param>
    /// <returns>An <see cref="ImportStatement"/>.</returns>
    public static ImportStatement GetImportStatement(this Type type, string targetPath, string relativePath, int segmentsToSkip)
    {
        var targetType = type.GetTargetType();
        var importPath = targetType.Module;
        if (!targetType.FromPackage)
        {
            if (string.IsNullOrEmpty(importPath))
            {
                var fullPath = Path.Join(targetPath, relativePath);
                var fullPathForType = Path.Join(targetPath, type.ResolveTargetPath(segmentsToSkip));
                var typeName = type.GetGeneratedModuleBaseName(_sourceFileMap);

                importPath = $"{Path.GetRelativePath(fullPath, fullPathForType)}/{GeneratedFileNames.ModuleNameFor(typeName)}";
            }

            if (!importPath.StartsWith('.') && !importPath.StartsWith('/'))
            {
                importPath = $"./{importPath}";
            }
        }
        return new ImportStatement(targetType.OriginalType, targetType.Type, importPath);
    }

    /// <summary>
    /// Get imports from a collection of types.
    /// </summary>
    /// <param name="types">Types to get from.</param>
    /// <param name="targetPath">The target path the proxies are generated to.</param>
    /// <param name="relativePath">The relative path to work from.</param>
    /// <param name="segmentsToSkip">Number of segments to skip from the namespace when generating the output path.</param>
    /// <returns>A collection of <see cref="ImportStatement"/>.</returns>
    public static IEnumerable<ImportStatement> GetImports(this IEnumerable<Type> types, string targetPath, string relativePath, int segmentsToSkip) =>
        [.. types.Select(_ => _.GetImportStatement(targetPath, relativePath, segmentsToSkip))];

    /// <summary>
    /// Collect types involved for a property, recursively.
    /// </summary>
    /// <param name="property">Property to collect for.</param>
    /// <param name="typesInvolved">Collected types involved.</param>
    /// <remarks>It skips any types already added to the collection passed to it.</remarks>
    public static void CollectTypesInvolved(this PropertyDescriptor property, IList<Type> typesInvolved)
    {
        if (property.OriginalType.IsDictionary())
        {
            CollectDictionaryValueTypesInvolved(property.OriginalType, typesInvolved);
            return;
        }

        if (IsNotCollectable(property.OriginalType, typesInvolved)) return;
        typesInvolved.Add(property.OriginalType);
        CollectDerivativeTypesInvolved(property.OriginalType, typesInvolved);
        CollectSubPropertyTypesInvolved(property.OriginalType, typesInvolved);
    }

    /// <summary>
    /// Collect types involved for an parameter, recursively.
    /// </summary>
    /// <param name="parameter">Parameter to collect for.</param>
    /// <param name="typesInvolved">Collected types involved.</param>
    /// <remarks>It skips any types already added to the collection passed to it.</remarks>
    public static void CollectTypesInvolved(this RequestParameterDescriptor parameter, IList<Type> typesInvolved)
    {
        if (typesInvolved.Contains(parameter.OriginalType) || IsKnownOrExternal(parameter.OriginalType)) return;
        typesInvolved.Add(parameter.OriginalType);
        CollectSubPropertyTypesInvolved(parameter.OriginalType, typesInvolved);
    }

    /// <summary>
    /// Collect types involved for a type, recursively.
    /// </summary>
    /// <param name="type">Type to collect for.</param>
    /// <param name="typesInvolved">Collected types involved.</param>
    /// <remarks>It skips any types already added to the collection passed to it.</remarks>
    public static void CollectTypesInvolved(this Type type, IList<Type> typesInvolved)
    {
        // A generic parameter is a placeholder rather than a type - `T` has no full name, and a descriptor
        // built from one is nameless, colliding with anything else that lands on the same path. An open
        // generic definition has no concrete shape to emit either; only its closed instantiations do.
        if (type.IsGenericParameter || type.IsGenericTypeDefinition) return;
        if (IsNotCollectable(type, typesInvolved)) return;
        typesInvolved.Add(type);

        // Collect base type for TypeScript inheritance support (skip system types and object)
        if (type.BaseType is { } baseType && baseType.IsValidInheritanceBaseType())
        {
            baseType.CollectTypesInvolved(typesInvolved);
        }

        CollectDerivativeTypesInvolved(type, typesInvolved);
        CollectSubPropertyTypesInvolved(type, typesInvolved);
    }

    static TargetType GetDictionaryTargetType(Type type)
    {
        var keyTargetType = GetDictionaryKeyTargetType(ResolveDictionaryKeyType(type.GetDictionaryKeyType()));
        var valueTypeStr = GetDictionaryValueTypeString(type.GetDictionaryValueType());

        if (keyTargetType.Type == "string")
        {
            return new(type, $"Record<string, {valueTypeStr}>", "Object", Final: true);
        }

        return new(type, $"ValueMap<{keyTargetType.Type}, {valueTypeStr}>", "ValueMap", Final: true);
    }

    static Type ResolveDictionaryKeyType(Type keyType) => keyType.IsConcept() ? keyType.GetConceptValueType() : keyType;

    static TargetType GetDictionaryKeyTargetType(Type resolvedKeyType) =>
        resolvedKeyType.IsEnum
            ? new TargetType(resolvedKeyType, resolvedKeyType.Name, "Number")
            : resolvedKeyType.FullName is not null && _primitiveTypeMap.TryGetValue(resolvedKeyType.FullName, out var primitiveKeyType)
                ? primitiveKeyType
                : new TargetType(resolvedKeyType, resolvedKeyType.Name, resolvedKeyType.Name);

    static bool TryGetKnownTargetType(Type type, out TargetType targetType)
    {
        // Ahead of the built-in map, which is what lets a mapping correct an existing type rather than
        // only add an unknown one. The built-in map is still what answers when nothing was configured.
        if (type.FullName is not null && _typeMappings.TryGetValue(type.FullName, out var mapped))
        {
            targetType = mapped with { OriginalType = type };
            return true;
        }

        if (type.FullName is not null && _primitiveTypeMap.TryGetValue(type.FullName, out var value))
        {
            targetType = value;
            return true;
        }

        targetType = null!;
        return false;
    }

    static string StripGenericArity(string typeName)
    {
        var backtickIndex = typeName.IndexOf('`');
        return backtickIndex >= 0 ? typeName[..backtickIndex] : typeName;
    }

    static void AddPropertyImports(PropertyDescriptor property, List<Type> typesInvolved, List<ImportStatement> imports)
    {
        if (!property.OriginalType.IsKnownType())
        {
            property.CollectTypesInvolved(typesInvolved);
        }

        if (property.OriginalType.IsDictionary())
        {
            AddDictionaryPropertyImports(property, typesInvolved, imports);
        }

        if (!string.IsNullOrEmpty(property.Module))
        {
            imports.Add(new ImportStatement(property.OriginalType, property.Type, property.Module));
        }
    }

    static void AddDictionaryPropertyImports(PropertyDescriptor property, List<Type> typesInvolved, List<ImportStatement> imports)
    {
        var keyType = property.OriginalType.GetDictionaryKeyType();
        var valueType = property.OriginalType.GetDictionaryValueType();
        var resolvedKeyType = ResolveDictionaryKeyType(keyType);
        if (!resolvedKeyType.IsAPrimitiveType() && !resolvedKeyType.IsEnum)
        {
            resolvedKeyType.CollectTypesInvolved(typesInvolved);
        }

        // If value type is a collection, collect the element type instead of the collection itself
        var effectiveValueType = valueType.GetDictionaryEffectiveValueType();
        if (!effectiveValueType.IsAPrimitiveType() && !effectiveValueType.IsConcept())
        {
            effectiveValueType.CollectTypesInvolved(typesInvolved);
        }

        var resolvedKeyTargetType = GetDictionaryKeyTargetType(resolvedKeyType);
        if (resolvedKeyTargetType.Type != "string")
        {
            imports.Add(new ImportStatement(property.OriginalType, "ValueMap", "@cratis/fundamentals"));
        }

        // A package-mapped key or value (for example Guid, TimeSpan, or a concept over one) still needs its
        // own import. It is not collected into typesInvolved above (primitives/concepts are skipped), and a
        // dictionary property's own Module is empty, so the general property import below never covers it —
        // leaving the generated Record/ValueMap referencing an undeclared type.
        if (resolvedKeyTargetType.FromPackage)
        {
            imports.Add(new ImportStatement(resolvedKeyType, resolvedKeyTargetType.Type, resolvedKeyTargetType.Module));
        }

        var effectiveValueTargetType = effectiveValueType.GetTargetType();
        if (effectiveValueTargetType.FromPackage)
        {
            imports.Add(new ImportStatement(effectiveValueType, effectiveValueTargetType.Type, effectiveValueTargetType.Module));
        }
    }

    /// <summary>
    /// Get the imports of the derivative types of interface-typed properties.
    /// </summary>
    /// <param name="propertyDescriptors">The property descriptors to get for.</param>
    /// <param name="targetPath">The target path the proxies are generated to.</param>
    /// <param name="relativePath">The relative path to work from.</param>
    /// <param name="segmentsToSkip">Number of segments to skip from the namespace when generating the output path.</param>
    /// <returns>The <see cref="ImportStatement"/> of every derivative type.</returns>
    /// <remarks>
    /// Derivative types are from mapped assemblies and are excluded from typesInvolved collection,
    /// so they never reach GetImports(). Without this, derivative constructor names are
    /// emitted in @field(...) but have no corresponding import statements.
    /// </remarks>
    static IEnumerable<ImportStatement> GetDerivativeImports(List<PropertyDescriptor> propertyDescriptors, string targetPath, string relativePath, int segmentsToSkip) =>
        propertyDescriptors
            .Where(pd => !string.IsNullOrEmpty(pd.Derivatives))
            .SelectMany(pd => pd.OriginalType.GetDerivativeTypes())
            .Select(derivativeType => derivativeType.GetImportStatement(targetPath, relativePath, segmentsToSkip));

    static IEnumerable<ImportStatement> GetReferencedImports(IEnumerable<ImportStatement> imports, List<PropertyDescriptor> propertyDescriptors, string? baseTypeName)
    {
        var derivativeConstructorNames = new HashSet<string>(
            propertyDescriptors
                .Where(pd => !string.IsNullOrEmpty(pd.Derivatives))
                .SelectMany(pd => pd.Derivatives.Split(", ")));

        bool IsReferenced(ImportStatement import) =>
            propertyDescriptors.Exists(pd => TypeNameIsReferenced(pd.Type, import.Type)) ||
            derivativeConstructorNames.Contains(import.Type) ||
            (!string.IsNullOrEmpty(baseTypeName) && import.Type == baseTypeName);

        return imports.Where(IsReferenced);
    }

#pragma warning disable MA0009 // Add regex evaluation timeout
    static bool TypeNameIsReferenced(string propertyType, string importTypeName) =>
        propertyType == importTypeName ||
        Regex.IsMatch(propertyType, $@"\b{Regex.Escape(importTypeName)}\b");
#pragma warning restore MA0009 // Add regex evaluation timeout

    static bool IsNotCollectable(Type type, IList<Type> typesInvolved) =>
        typesInvolved.Contains(type) || type.IsAPrimitiveType() || type.IsConcept() || IsKnownOrExternal(type);

    static bool IsKnownOrExternal(Type type) => type.IsKnownType() || type.IsFromMappedAssembly() || type.IsExcluded();

    static void CollectDictionaryValueTypesInvolved(Type dictionaryType, IList<Type> typesInvolved)
    {
        var valueType = dictionaryType.GetDictionaryValueType();

        // If value type is a collection, collect the element type instead of the collection itself
        var effectiveValueType = valueType.GetDictionaryEffectiveValueType();
        if (!effectiveValueType.IsAPrimitiveType() && !effectiveValueType.IsConcept())
        {
            effectiveValueType.CollectTypesInvolved(typesInvolved);
        }
    }

    static void CollectDerivativeTypesInvolved(Type type, IList<Type> typesInvolved)
    {
        foreach (var derivativeType in type.GetDerivativeTypes())
        {
            derivativeType.CollectTypesInvolved(typesInvolved);
        }
    }

    static void CollectSubPropertyTypesInvolved(Type type, IList<Type> typesInvolved)
    {
        foreach (var subProperty in type.GetPropertyDescriptors().Where(_ => !_.OriginalType.IsKnownType() || _.OriginalType.IsDictionary()))
        {
            CollectTypesInvolved(subProperty, typesInvolved);
        }
    }

    static string ResolveNamespaceRootPath(string ns, (string Namespace, string Folder) root)
    {
        var (rootNs, folder) = root;
        var remainder = ns == rootNs ? string.Empty : ns[(rootNs.Length + 1)..];
        var subPath = string.IsNullOrEmpty(remainder)
            ? string.Empty
            : string.Join(Path.DirectorySeparatorChar, remainder.Split('.'));

        if (string.IsNullOrEmpty(folder))
        {
            return string.IsNullOrEmpty(subPath) ? $"{Path.DirectorySeparatorChar}" : subPath;
        }

        return string.IsNullOrEmpty(subPath) ? folder : Path.Join(folder, subPath);
    }
}
