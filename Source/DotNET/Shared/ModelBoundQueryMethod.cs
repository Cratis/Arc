// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Adapts query selection to reflection, including MetadataLoadContext.
/// </summary>
internal static class ModelBoundQueryMethod
{
    /// <summary>
    /// Full metadata name of the owner attribute.
    /// </summary>
    internal const string ReadModelName = QuerySelectionPolicy.ReadModelName;

    /// <summary>
    /// Full metadata name of the method marker.
    /// </summary>
    internal const string QueryName = QuerySelectionPolicy.QueryName;

    /// <summary>
    /// Tests the public static reflection candidate shape.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="findAttribute">The discovery-scoped marker probe, or null for reflection defaults.</param>
    /// <returns>Whether the method is a candidate.</returns>
    internal static bool IsCandidate(MethodInfo method, Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute = null) =>
        method.IsPublic &&
        method.IsStatic &&
        !method.IsSpecialName &&
        !method.Name.Contains('<') &&
        !IsCompilerGenerated(method, findAttribute);

    /// <summary>
    /// Reads the Arc marker by its full metadata name.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="findAttribute">The discovery-scoped marker probe, or null for reflection defaults.</param>
    /// <returns>Whether the method is marked.</returns>
    internal static bool IsMarked(MethodInfo method, Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute = null) => AttributeOf(method, QueryName, findAttribute) is not null;

    /// <summary>
    /// Reads the nearest owner declaration, including a direct default-mode reset.
    /// </summary>
    /// <param name="type">The owner.</param>
    /// <param name="findAttribute">The discovery-scoped marker probe, or null for reflection defaults.</param>
    /// <returns>The nearest read-model attribute.</returns>
    internal static CustomAttributeData? ReadModel(Type type, Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute = null)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var attribute = AttributeOf(current, ReadModelName, findAttribute);
            if (attribute is not null)
            {
                return attribute;
            }
        }

        return null;
    }

    /// <summary>
    /// Reads the opt-in setting from the nearest read-model declaration.
    /// </summary>
    /// <param name="type">The owner.</param>
    /// <param name="findAttribute">The discovery-scoped marker probe, or null for reflection defaults.</param>
    /// <returns>Whether explicit selection is required.</returns>
    internal static bool RequiresQueryAttribute(Type type, Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute = null) =>
        ReadModel(type, findAttribute)?.NamedArguments.Any(_ => _.MemberName == "RequireQueryAttribute" && _.TypedValue.Value is true) == true;

    /// <summary>
    /// Requires public static visibility before applying opt-in selection.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="owner">The discovery owner.</param>
    /// <param name="findAttribute">The discovery-scoped marker probe, or null for reflection defaults.</param>
    /// <returns>Whether selection permits the method.</returns>
    internal static bool IsSelected(MethodInfo method, Type owner, Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute = null) =>
        method.IsPublic && method.IsStatic && QuerySelectionPolicy.IsSelected(HasSelfParameter(method, owner), RequiresQueryAttribute(owner, findAttribute), () => IsMarked(method, findAttribute), () => IsValidExplicitQuery(method, findAttribute));

    /// <summary>
    /// Validates explicit declarations before selecting endpoints, preserving the caller's legacy return predicate.
    /// </summary>
    /// <param name="owner">The discovery owner.</param>
    /// <param name="legacyReturn">The legacy return predicate.</param>
    /// <param name="findAttribute">The discovery-scoped marker probe, or null for reflection defaults.</param>
    /// <returns>The selected methods.</returns>
    /// <exception cref="InvalidQueryDeclaration">An explicit query declaration is invalid or ambiguous.</exception>
    internal static MethodInfo[] GetMethods(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type owner,
        Func<MethodInfo, bool> legacyReturn,
        Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute = null) =>
        GetMethods(owner, owner.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance), legacyReturn, findAttribute);

    /// <summary>
    /// Selects queries from an already discovered method set using the surface's conventional return predicate.
    /// </summary>
    /// <param name="owner">The discovery owner.</param>
    /// <param name="methods">All public and nonpublic static and instance methods.</param>
    /// <param name="legacyReturn">The conventional return predicate.</param>
    /// <param name="findAttribute">The discovery-scoped marker probe, or null for reflection defaults.</param>
    /// <returns>The selected methods.</returns>
    /// <exception cref="InvalidQueryDeclaration">An explicit query declaration is invalid or ambiguous.</exception>
    internal static MethodInfo[] GetMethods(Type owner, MethodInfo[] methods, Func<MethodInfo, bool> legacyReturn, Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute = null)
    {
        Validate(owner, methods, legacyReturn, findAttribute);
        var explicitMode = RequiresQueryAttribute(owner, findAttribute);
        var selected = methods
            .Where(method => IsCandidate(method, findAttribute) && IsSelected(method, owner, findAttribute) && legacyReturn(method))
            .ToArray();
        var duplicate = explicitMode ? selected.GroupBy(_ => _.Name).FirstOrDefault(_ => _.Count() > 1) : null;
        if (duplicate is not null)
        {
            throw new InvalidQueryDeclaration(owner, duplicate.Key, "only one selected query per method name is supported; rename a query or remove its [Query] marker");
        }

        return selected;
    }

    /// <summary>
    /// Rejects invalid markers even when their owner or method would not be discovered.
    /// </summary>
    /// <param name="owner">The declaring type.</param>
    /// <param name="legacyReturn">The conventional return predicate.</param>
    /// <param name="findAttribute">The discovery-scoped marker probe, or null for reflection defaults.</param>
    /// <exception cref="InvalidQueryDeclaration">A marked query declaration is invalid.</exception>
    internal static void Validate(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type owner,
        Func<MethodInfo, bool> legacyReturn,
        Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute = null) =>
        Validate(owner, owner.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance), legacyReturn, findAttribute);

    /// <summary>
    /// Rejects invalid markers in an already discovered method set.
    /// </summary>
    /// <param name="owner">The declaring type.</param>
    /// <param name="methods">All public and nonpublic static and instance methods.</param>
    /// <param name="legacyReturn">The conventional return predicate.</param>
    /// <param name="findAttribute">The discovery-scoped marker probe, or null for reflection defaults.</param>
    /// <exception cref="InvalidQueryDeclaration">A marked query declaration is invalid.</exception>
    internal static void Validate(Type owner, MethodInfo[] methods, Func<MethodInfo, bool> legacyReturn, Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute = null)
    {
        foreach (var method in methods.Where(method => IsMarked(method, findAttribute)))
        {
            if (ReadModel(owner, findAttribute) is null || HasSelfParameter(method, owner) || !IsValidExplicitQuery(method, findAttribute) || !legacyReturn(method))
            {
                var reason = HasSelfParameter(method, owner)
                    ? QuerySelectionPolicy.SelfParameterReason
                    : "[Query] requires a public static nongeneric ordinary method with value parameters and a supported read-model return shape on [ReadModel]";
                throw new InvalidQueryDeclaration(owner, method.Name, reason);
            }
        }
    }

    /// <summary>
    /// Validates the structural explicit query rules; the discovery surface validates the return shape.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="findAttribute">The discovery-scoped marker probe, or null for reflection defaults.</param>
    /// <returns>Whether the method is eligible in explicit mode.</returns>
    internal static bool IsValidExplicitQuery(MethodInfo method, Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute = null) =>
        IsCandidate(method, findAttribute) && !method.ContainsGenericParameters &&
        !method.ReturnType.IsByRef && method.GetParameters().All(_ => !_.ParameterType.IsByRef) &&
        !HasSelfParameter(method, method.DeclaringType!);

    /// <summary>
    /// Detects an exact self parameter, including nullable value types in MetadataLoadContext.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="owner">The read model.</param>
    /// <returns>Whether any parameter is the read model itself.</returns>
    internal static bool HasSelfParameter(MethodInfo method, Type owner) =>
        method.GetParameters().Any(parameter => UnwrapNullable(parameter.ParameterType.IsByRef
            ? parameter.ParameterType.GetElementType()!
            : parameter.ParameterType) == owner);

    static Type UnwrapNullable(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition().FullName == "System.Nullable`1"
            ? type.GetGenericArguments()[0]
            : type;

    static CustomAttributeData? AttributeOf(MemberInfo member, string fullName, Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute = null)
    {
        if (findAttribute is not null)
        {
            return findAttribute(member, fullName, true);
        }

#if METADATA_QUERY_ATTRIBUTES
        if (member.Module.Assembly.ReflectionOnly)
        {
            return MetadataQueryAttributes.FindUncached(member, fullName);
        }
#endif
        return member.GetCustomAttributesData().FirstOrDefault(_ => _.AttributeType.FullName == fullName);
    }

    static bool IsCompilerGenerated(MethodInfo method, Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute = null)
    {
        if (findAttribute is not null)
        {
            return findAttribute(method, "CompilerGeneratedAttribute", false) is not null;
        }

#if METADATA_QUERY_ATTRIBUTES
        if (method.Module.Assembly.ReflectionOnly)
        {
            return MetadataQueryAttributes.FindUncached(method, "CompilerGeneratedAttribute", matchFullName: false) is not null;
        }
#endif
        return method.GetCustomAttributesData().Any(_ => _.AttributeType.Name == "CompilerGeneratedAttribute");
    }

    /// <summary>
    /// The exception that is thrown when explicit query intent is invalid or ambiguous.
    /// </summary>
    /// <param name="owner">The discovery owner.</param>
    /// <param name="method">The method name.</param>
    /// <param name="reason">The actionable error.</param>
    public sealed class InvalidQueryDeclaration(Type owner, string method, string reason) : Exception($"Invalid query '{owner.FullName}.{method}': {reason}.");
}
