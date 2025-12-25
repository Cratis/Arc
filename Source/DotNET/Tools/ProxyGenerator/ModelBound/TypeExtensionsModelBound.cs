// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

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
    public static bool IsCommand(this Type type) =>
        type.IsClass &&
        !type.IsAbstract &&
        type.GetCustomAttributesData()
            .Select(_ => _.AttributeType.Name)
            .Any(_ => _ == WellKnownTypes.CommandAttribute) &&
        type.HasHandleMethod();

    /// <summary>
    /// Determine if a type is a model bound read model (query).
    /// </summary>
    /// <param name="type">Type to inspect.</param>
    /// <returns>True if the type is a model bound read model, false otherwise.</returns>
    public static bool IsQuery(this Type type) =>
        type.IsClass &&
        !type.IsAbstract &&
        type.GetCustomAttributesData()
            .Select(_ => _.AttributeType.Name)
            .Any(_ => _ == WellKnownTypes.ReadModelAttribute) &&
        type.HasQueryMethods();

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
    /// <returns>True if the type has public static methods, false otherwise.</returns>
    public static bool HasQueryMethods(this Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Any(_ => !_.IsSpecialName && _.IsValidQueryFor(type));

    /// <summary>
    /// Check if a method qualifies as a query performer for the specified read model type.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <param name="readModelType">The read model type to validate against.</param>
    /// <returns>True if the method qualifies as a query performer; otherwise, false.</returns>
    public static bool IsValidQueryFor(this MethodInfo method, Type readModelType)
    {
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
    /// <returns>Collection of public static methods.</returns>
    public static IEnumerable<MethodInfo> GetQueryMethods(this Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Where(_ => !_.IsSpecialName && _.IsValidQueryFor(type));

    static bool IsCollectionOfType(Type type, Type elementType)
    {
        if (type.IsArray && type.GetElementType() == elementType)
        {
            return true;
        }

        return type.ImplementsEnumerable();
    }
}