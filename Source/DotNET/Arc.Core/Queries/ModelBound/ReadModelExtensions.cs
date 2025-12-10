// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Reflection;
using Cratis.Reflection;

namespace Cratis.Arc.Queries.ModelBound;

/// <summary>
/// Extension methods for the <see cref="ReadModelAttribute"/>.
/// </summary>
public static class ReadModelExtensions
{
    /// <summary>
    /// Check if a type is a read model.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if the type is a read model; otherwise, false.</returns>
    public static bool IsReadModel(this Type type) => type.HasAttribute<ReadModelAttribute>();

    /// <summary>
    /// Check if a method qualifies as a query performer for the specified read model type.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <param name="readModelType">The read model type to validate against.</param>
    /// <returns>True if the method qualifies as a query performer; otherwise, false.</returns>
    public static bool IsValidQueryFor(this MethodInfo method, Type readModelType)
    {
        var returnType = method.ReturnType;

        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
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
            returnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>) &&
            returnType.GetGenericArguments()[0] == readModelType)
        {
            return true;
        }

        if (returnType.IsGenericType &&
            returnType.GetGenericTypeDefinition() == typeof(ISubject<>) &&
            returnType.GetGenericArguments()[0] == readModelType)
        {
            return true;
        }

        if (returnType.IsGenericType &&
            returnType.GetGenericTypeDefinition() == typeof(ISubject<>) &&
            IsCollectionOfType(returnType.GetGenericArguments()[0], readModelType))
        {
            return true;
        }

        return false;
    }

    static bool IsCollectionOfType(Type type, Type elementType)
    {
        if (type.IsArray && type.GetElementType() == elementType)
        {
            return true;
        }

        return type
            .IsAssignableTo(typeof(IEnumerable<>)
            .MakeGenericType(elementType));
    }
}
