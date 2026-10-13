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
    /// Tests the legacy reflection candidate shape without changing its eligibility rules.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns>Whether the method is a candidate.</returns>
    internal static bool IsCandidate(MethodInfo method) =>
        (method.IsPublic || method.IsAssembly) &&
        method.IsStatic &&
        !method.IsSpecialName &&
        !method.Name.Contains('<') &&
        !IsCompilerGenerated(method);

    /// <summary>
    /// Reads the Arc marker by its full metadata name.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns>Whether the method is marked.</returns>
    internal static bool IsMarked(MethodInfo method) => AttributeOf(method, QueryName) is not null;

    /// <summary>
    /// Reads the nearest owner declaration, including a direct default-mode reset.
    /// </summary>
    /// <param name="type">The owner.</param>
    /// <returns>The nearest read-model attribute.</returns>
    internal static CustomAttributeData? ReadModel(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var attribute = AttributeOf(current, ReadModelName);
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
    /// <returns>Whether explicit selection is required.</returns>
    internal static bool RequiresQueryAttribute(Type type) =>
        ReadModel(type)?.NamedArguments.Any(_ => _.MemberName == "RequireQueryAttribute" && _.TypedValue.Value is true) == true;

    /// <summary>
    /// Applies opt-in selection without narrowing legacy methods.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="owner">The discovery owner.</param>
    /// <returns>Whether selection permits the method.</returns>
    internal static bool IsSelected(MethodInfo method, Type owner) =>
        QuerySelectionPolicy.IsSelected(HasSelfParameter(method, owner), RequiresQueryAttribute(owner), () => IsMarked(method), () => IsValidExplicitQuery(method));

    /// <summary>
    /// Validates explicit declarations before selecting endpoints, preserving the caller's legacy return predicate.
    /// </summary>
    /// <param name="owner">The discovery owner.</param>
    /// <param name="legacyReturn">The legacy return predicate.</param>
    /// <returns>The selected methods.</returns>
    /// <exception cref="InvalidQueryDeclaration">An explicit query declaration is invalid or ambiguous.</exception>
    internal static MethodInfo[] GetMethods(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type owner,
        Func<MethodInfo, bool> legacyReturn) =>
        GetMethods(owner, owner.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance), legacyReturn);

    /// <summary>
    /// Selects queries from an already discovered method set using the surface's conventional return predicate.
    /// </summary>
    /// <param name="owner">The discovery owner.</param>
    /// <param name="methods">All public and nonpublic static and instance methods.</param>
    /// <param name="legacyReturn">The conventional return predicate.</param>
    /// <returns>The selected methods.</returns>
    /// <exception cref="InvalidQueryDeclaration">An explicit query declaration is invalid or ambiguous.</exception>
    internal static MethodInfo[] GetMethods(Type owner, MethodInfo[] methods, Func<MethodInfo, bool> legacyReturn)
    {
        Validate(owner, methods, legacyReturn);
        var explicitMode = RequiresQueryAttribute(owner);
        var selected = methods
            .Where(method => IsCandidate(method) && IsSelected(method, owner) && legacyReturn(method))
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
    /// <exception cref="InvalidQueryDeclaration">A marked query declaration is invalid.</exception>
    internal static void Validate(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type owner,
        Func<MethodInfo, bool> legacyReturn) =>
        Validate(owner, owner.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance), legacyReturn);

    /// <summary>
    /// Rejects invalid markers in an already discovered method set.
    /// </summary>
    /// <param name="owner">The declaring type.</param>
    /// <param name="methods">All public and nonpublic static and instance methods.</param>
    /// <param name="legacyReturn">The conventional return predicate.</param>
    /// <exception cref="InvalidQueryDeclaration">A marked query declaration is invalid.</exception>
    internal static void Validate(Type owner, MethodInfo[] methods, Func<MethodInfo, bool> legacyReturn)
    {
        foreach (var method in methods.Where(IsMarked))
        {
            if (ReadModel(owner) is null || HasSelfParameter(method, owner) || !IsValidExplicitQuery(method) || !legacyReturn(method))
            {
                throw new InvalidQueryDeclaration(owner, method.Name, HasSelfParameter(method, owner)
                    ? QuerySelectionPolicy.SelfParameterReason
                    : "[Query] requires a public/internal static nongeneric ordinary method with value parameters and a supported read-model return shape on [ReadModel]");
            }
        }
    }

    /// <summary>
    /// Validates the structural explicit query rules; the discovery surface validates the return shape.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <returns>Whether the method is eligible in explicit mode.</returns>
    internal static bool IsValidExplicitQuery(MethodInfo method) =>
        IsCandidate(method) && !method.ContainsGenericParameters &&
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

    static CustomAttributeData? AttributeOf(MemberInfo member, string fullName)
    {
#if METADATA_QUERY_ATTRIBUTES
        if (member.Module.Assembly.ReflectionOnly)
        {
            return MetadataQueryAttributes.Find(member, fullName);
        }
#endif
        return member.GetCustomAttributesData().FirstOrDefault(_ => _.AttributeType.FullName == fullName);
    }

    static bool IsCompilerGenerated(MethodInfo method)
    {
#if METADATA_QUERY_ATTRIBUTES
        if (method.Module.Assembly.ReflectionOnly)
        {
            return MetadataQueryAttributes.Find(method, "CompilerGeneratedAttribute", matchFullName: false) is not null;
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
