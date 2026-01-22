// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reflection;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Maps a concept type to the underlying primitive type.
/// </summary>
public static class ConceptMap
{
    static readonly ConcurrentDictionary<Type, Type> _primitiveTypeCache = new();
    static readonly ConcurrentDictionary<Type, PropertyInfo> _valuePropertyCache = new();

    /// <summary>
    /// Get the type of the value in a concept.
    /// </summary>
    /// <param name="type"><see cref="Type"/> to get value type from.</param>
    /// <returns>The type of the concept value.</returns>
    public static Type GetConceptValueType(Type type)
    {
        if (_primitiveTypeCache.TryGetValue(type, out var value)) return value;

        var primitiveType = GetPrimitiveType(type);
        _primitiveTypeCache.TryAdd(type, primitiveType);
        return primitiveType;
    }

    /// <summary>
    /// Get the <see cref="PropertyInfo"/> for the value property in a concept.
    /// </summary>
    /// <param name="type">Type to get for.</param>
    /// <returns><see cref="PropertyInfo"/> for the concept type.</returns>
    public static PropertyInfo GetValuePropertyInfo(Type type)
    {
        if (_valuePropertyCache.TryGetValue(type, out var value)) return value;

        var valueProperty = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
        _valuePropertyCache.TryAdd(type, valueProperty!);
        return valueProperty!;
    }

    static Type GetPrimitiveType(Type type)
    {
        var conceptType = type;
        for (; ; )
        {
            if (conceptType == TypeExtensions._conceptType || conceptType.BaseType == null) break;

            if (conceptType!.BaseType!.IsGenericType)
            {
                var genericTypeDef = conceptType.BaseType.GetGenericTypeDefinition();

                // Check by type reference (MetadataLoadContext types)
                if (genericTypeDef == TypeExtensions._conceptType)
                {
                    return conceptType.BaseType.GetGenericArguments()[0];
                }

                // Fallback: Check by type name for runtime types
                if (genericTypeDef.FullName == "Cratis.Concepts.ConceptAs`1")
                {
                    return conceptType.BaseType.GetGenericArguments()[0];
                }
            }

            if (conceptType == typeof(object)) break;

            conceptType = conceptType.GetTypeInfo().BaseType!;
        }

        return typeof(void);
    }
}
