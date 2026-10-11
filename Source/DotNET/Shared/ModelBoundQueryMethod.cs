// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        !(method.Module.Assembly.ReflectionOnly
            ? MetadataQueryAttributes.Find(method, "CompilerGeneratedAttribute", matchFullName: false) is not null
            : method.GetCustomAttributesData().Any(_ => _.AttributeType.Name == "CompilerGeneratedAttribute"));

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
        QuerySelectionPolicy.IsSelected(RequiresQueryAttribute(owner), () => IsMarked(method), () => IsValidExplicitQuery(method, owner));

    /// <summary>
    /// Validates explicit declarations before selecting endpoints, preserving the caller's legacy return predicate.
    /// </summary>
    /// <param name="owner">The discovery owner.</param>
    /// <param name="legacyReturn">The legacy return predicate.</param>
    /// <returns>The selected methods.</returns>
    /// <exception cref="InvalidQueryDeclaration">An explicit query declaration is invalid or ambiguous.</exception>
    internal static MethodInfo[] GetMethods(Type owner, Func<MethodInfo, bool> legacyReturn)
    {
        Validate(owner);
        var explicitMode = RequiresQueryAttribute(owner);
        var selected = owner.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(method => IsCandidate(method) && (explicitMode ? IsSelected(method, owner) : legacyReturn(method)))
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
    /// <exception cref="InvalidQueryDeclaration">A marked query declaration is invalid.</exception>
    internal static void Validate(Type owner)
    {
        var all = owner.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        foreach (var method in all.Where(IsMarked))
        {
            if (ReadModel(owner) is null || !IsValidExplicitQuery(method, owner))
            {
                throw new InvalidQueryDeclaration(owner, method.Name, "[Query] requires a public/internal static nongeneric ordinary method with value parameters and a supported read-model return shape on [ReadModel]");
            }
        }
    }

    /// <summary>
    /// Validates the complete explicit query shape.
    /// </summary>
    /// <param name="method">The method.</param>
    /// <param name="owner">The discovery owner.</param>
    /// <returns>Whether the method is eligible in explicit mode.</returns>
    internal static bool IsValidExplicitQuery(MethodInfo method, Type owner) =>
        IsCandidate(method) && !method.ContainsGenericParameters &&
        !method.ReturnType.IsByRef && method.GetParameters().All(_ => !_.ParameterType.IsByRef) &&
        IsSupportedReturn(method.ReturnType, owner);

    static CustomAttributeData? AttributeOf(MemberInfo member, string fullName) => member.Module.Assembly.ReflectionOnly
        ? MetadataQueryAttributes.Find(member, fullName)
        : member.GetCustomAttributesData().FirstOrDefault(_ => _.AttributeType.FullName == fullName);

    static bool IsSupportedReturn(Type type, Type owner)
    {
        if (IsGeneric(type, "System.Threading.Tasks.Task`1"))
        {
            type = type.GetGenericArguments()[0];
        }

        return type == owner || IsCollection(type, owner) ||
            (IsGeneric(type, "System.Collections.Generic.IAsyncEnumerable`1") && type.GetGenericArguments()[0] == owner) ||
            (IsGeneric(type, "System.Reactive.Subjects.ISubject`1") &&
                (type.GetGenericArguments()[0] == owner || IsCollection(type.GetGenericArguments()[0], owner)));
    }

    static bool IsCollection(Type type, Type owner) =>
        (type.IsArray && type.GetElementType() == owner) ||
        new[] { type }.Concat(type.GetInterfaces()).Any(_ => IsGeneric(_, "System.Collections.Generic.IEnumerable`1") && _.GetGenericArguments()[0] == owner);

    static bool IsGeneric(Type type, string name) => type.IsGenericType && type.GetGenericTypeDefinition().FullName == name;

    /// <summary>
    /// The exception that is thrown when explicit query intent is invalid or ambiguous.
    /// </summary>
    /// <param name="owner">The discovery owner.</param>
    /// <param name="method">The method name.</param>
    /// <param name="reason">The actionable error.</param>
    public sealed class InvalidQueryDeclaration(Type owner, string method, string reason) : Exception($"Invalid query '{owner.FullName}.{method}': {reason}.");
}
