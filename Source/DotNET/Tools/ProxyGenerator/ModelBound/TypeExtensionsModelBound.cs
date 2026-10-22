// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.ModelBound;

/// <summary>
/// Extension methods for working with model bound types.
/// </summary>
public static class TypeExtensionsModelBound
{
    /// <summary>
    /// Determine if a type is a model bound command.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>True if the type is a model bound command, false otherwise.</returns>
    public static bool IsCommand(this Type type)
    {
        try
        {
            return type.IsClass &&
                !type.IsAbstract &&
                type.GetCustomAttributesData()
                    .Select(_ => _.AttributeType.Name)
                    .Any(_ => _ == WellKnownTypes.CommandAttribute) &&
                type.HasHandleMethod();
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Determine if a type is a model bound read model (query).
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>True if the type is a model bound read model, false otherwise.</returns>
    public static bool IsQuery(this Type type) => IsQuery(type, null);

    /// <summary>
    /// Determine if a type has a Handle method.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>True if the type has a Handle method, false otherwise.</returns>
    public static bool HasHandleMethod(this Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SingleOrDefault(_ => _.Name == "Handle") != null;

    /// <summary>
    /// Determine if a type has public static methods that can be queries.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>True if the type has public static query methods, false otherwise.</returns>
    public static bool HasQueryMethods(this Type type) =>
        type.GetQueryMethods().Any();

    /// <summary>
    /// Check if a method qualifies as a query performer for the specified read model type.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <param name="readModelType">The read model type to validate against.</param>
    /// <returns>True if the method qualifies as a query performer; otherwise, false.</returns>
    public static bool IsValidQueryFor(this MethodInfo method, Type readModelType) => IsValidQueryFor(method, readModelType, null);

    /// <summary>
    /// Get the Handle method from a type.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>The Handle method.</returns>
    public static MethodInfo GetHandleMethod(this Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Single(_ => _.Name == "Handle");

    /// <summary>
    /// Get all public static methods from a type that can be queries.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>Collection of public static query methods.</returns>
    public static IEnumerable<MethodInfo> GetQueryMethods(this Type type) =>
        GetQueryMethods(type, null);

    /// <summary>
    /// Determines query ownership within a metadata discovery scope.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <param name="findAttribute">The discovery-scoped marker probe.</param>
    /// <returns>Whether the type owns queries.</returns>
    internal static bool IsQuery(this Type type, Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute)
    {
        ModelBoundQueryMethod.Validate(type, method => method.IsValidQueryFor(type, findAttribute), findAttribute);
        return type.IsClass && (!type.IsAbstract || ModelBoundQueryMethod.RequiresQueryAttribute(type, findAttribute)) &&
            (ModelBoundQueryMethod.RequiresQueryAttribute(type, findAttribute)
                ? ModelBoundQueryMethod.ReadModel(type, findAttribute) is not null
                : HasConventionalReadModelAttribute(type, findAttribute)) &&
            type.GetQueryMethods(findAttribute).Any();
    }

    /// <summary>
    /// Checks a query candidate within a metadata discovery scope.
    /// </summary>
    /// <param name="method">The method to inspect.</param>
    /// <param name="readModelType">The read model type.</param>
    /// <param name="findAttribute">The discovery-scoped marker probe.</param>
    /// <returns>Whether the method qualifies as a query.</returns>
    internal static bool IsValidQueryFor(this MethodInfo method, Type readModelType, Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute)
    {
        // Mirrors the runtime predicate in Cratis.Arc.Queries.ModelBound.ReadModelExtensions: an open generic method
        // can never be invoked as a query, so emitting a client for it would promise a call that always fails.
        if (method.ContainsGenericParameters)
        {
            return false;
        }

        if (!ModelBoundQueryMethod.IsSelected(method, readModelType, findAttribute))
        {
            return false;
        }

        var returnType = method.ReturnType;

        if (returnType.IsGenericType &&
            returnType.IsTask())
        {
            returnType = returnType.GetGenericArguments()[0];
        }

        if (returnType == readModelType)
        {
            return true;
        }

        if (IsCollectionOfType(returnType, readModelType))
        {
            return true;
        }

        if (returnType.IsGenericType &&
            returnType.IsSubject() &&
            returnType.GetGenericArguments()[0] == readModelType)
        {
            return true;
        }

        if (returnType.IsGenericType &&
            returnType.IsSubject() &&
            IsCollectionOfType(returnType.GetGenericArguments()[0], readModelType))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Selects query methods within a metadata discovery scope.
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <param name="findAttribute">The discovery-scoped marker probe.</param>
    /// <returns>The selected query methods.</returns>
    internal static IEnumerable<MethodInfo> GetQueryMethods(this Type type, Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute) =>
        ModelBoundQueryMethod.GetMethods(type, method => method.IsValidQueryFor(type, findAttribute), findAttribute);

    static bool HasConventionalReadModelAttribute(Type type, Func<MemberInfo, string, bool, CustomAttributeData?>? findAttribute) => findAttribute is not null
        ? findAttribute(type, WellKnownTypes.ReadModelAttribute, false) is not null
        : type.Assembly.ReflectionOnly
            ? MetadataQueryAttributes.FindUncached(type, WellKnownTypes.ReadModelAttribute, matchFullName: false) is not null
            : type.GetCustomAttributesData().Any(_ => _.AttributeType.Name == WellKnownTypes.ReadModelAttribute);

    static bool IsCollectionOfType(Type type, Type elementType)
    {
        if (type.IsArray)
        {
            return type.GetElementType() == elementType;
        }

        // Compare names for the generic definition, but compare the argument as a Type: both it and the read
        // model belong to the same MetadataLoadContext, while typeof(IEnumerable<>) does not.
        return new[] { type }.Concat(type.GetInterfaces())
            .Any(_ => _.IsGenericType &&
                _.GetGenericTypeDefinition().FullName == "System.Collections.Generic.IEnumerable`1" &&
                elementType.IsAssignableFrom(_.GetGenericArguments()[0]));
    }
}