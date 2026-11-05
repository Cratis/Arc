// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Concepts;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Maps the records a Cypher query returns to strongly typed results.
/// </summary>
/// <remarks>
/// <para>
/// A record with one column maps that column's value when the result type is a primitive, a concept, an enum or a list
/// of those; otherwise, and for records with several columns, the result type is built from the columns by name. Names are
/// matched ignoring case, using the constructor parameters when there is a constructor with parameters, else the
/// settable properties. A column holding a node or a relationship contributes its properties.
/// </para>
/// <para>
/// A <see cref="ConceptAs{T}"/> property is built from the primitive the graph stores. The graph has no
/// <see cref="Guid"/>, enum or <see cref="decimal"/>, so they are read from their string and number forms.
/// </para>
/// </remarks>
public static class GraphRecordMapper
{
    /// <summary>
    /// Map a record to a result.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="record">The <see cref="IRecord"/> to map.</param>
    /// <returns>The result.</returns>
    public static T Map<T>(IRecord record) => (T)Map(record, typeof(T))!;

    /// <summary>
    /// Map a record to a result.
    /// </summary>
    /// <param name="record">The <see cref="IRecord"/> to map.</param>
    /// <param name="type">The type of the result.</param>
    /// <returns>The result.</returns>
    public static object? Map(IRecord record, Type type)
    {
        if (record.Keys.Count == 1 && IsValueType(type))
        {
            return ConvertValue(record[0], type);
        }

        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in record.Keys)
        {
            if (record[key] is IEntity entity && record.Keys.Count == 1)
            {
                foreach (var property in entity.Properties)
                {
                    values[property.Key] = property.Value;
                }
            }
            else
            {
                values[key] = record[key];
            }
        }

        return Build(values, type);
    }

    /// <summary>
    /// Convert a value from the graph to a type.
    /// </summary>
    /// <param name="value">The value the driver gave.</param>
    /// <param name="type">The type to convert to.</param>
    /// <returns>The converted value.</returns>
    public static object? ConvertValue(object? value, Type type)
    {
        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (value is null)
        {
            return target.IsValueType && Nullable.GetUnderlyingType(type) is null ? Activator.CreateInstance(target) : null;
        }

        if (target.IsConcept())
        {
            return ConceptFactory.CreateConceptInstance(target, ConvertValue(value, target.GetConceptValueType()));
        }

        if (target == typeof(object) || target.IsInstanceOfType(value))
        {
            return value;
        }

        if (value is IEntity entity)
        {
            return Build(new Dictionary<string, object?>(entity.Properties, StringComparer.OrdinalIgnoreCase), target);
        }

        if (target == typeof(Guid))
        {
            return Guid.Parse(value.ToString()!);
        }

        if (target.IsEnum)
        {
            return value is string name ? Enum.Parse(target, name, true) : Enum.ToObject(target, value);
        }

        if (target == typeof(DateTimeOffset) && value is DateTime dateTime)
        {
            return new DateTimeOffset(dateTime);
        }

        if (TryConvertCollection(value, target, out var collection))
        {
            return collection;
        }

        if (value is IDictionary<string, object?> map)
        {
            return Build(new Dictionary<string, object?>(map, StringComparer.OrdinalIgnoreCase), target);
        }

        return System.Convert.ChangeType(value, target, System.Globalization.CultureInfo.InvariantCulture);
    }

    static bool IsValueType(Type type)
    {
        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (target.IsConcept() || target.IsPrimitive || target.IsEnum || target == typeof(string) || target == typeof(decimal)
            || target == typeof(Guid) || target == typeof(DateTime) || target == typeof(DateTimeOffset) || target == typeof(object))
        {
            return true;
        }

        return target != typeof(string) && TryGetElementType(target, out var element) && IsValueType(element);
    }

    static object Build(Dictionary<string, object?> values, Type type)
    {
        var constructor = type.GetConstructors().OrderByDescending(_ => _.GetParameters().Length).FirstOrDefault();
        if (constructor?.GetParameters().Length > 0)
        {
            var arguments = constructor.GetParameters()
                .Select(parameter => values.TryGetValue(parameter.Name!, out var value)
                    ? ConvertValue(value, parameter.ParameterType)
                    : ConvertValue(null, parameter.ParameterType))
                .ToArray();
            return constructor.Invoke(arguments);
        }

        var instance = Activator.CreateInstance(type)!;
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(_ => _.CanWrite))
        {
            if (values.TryGetValue(property.Name, out var value))
            {
                property.SetValue(instance, ConvertValue(value, property.PropertyType));
            }
        }

        return instance;
    }

    static bool TryConvertCollection(object value, Type target, out object? collection)
    {
        collection = null;
        if (value is string || value is not IEnumerable source || !TryGetElementType(target, out var elementType))
        {
            return false;
        }

        var items = source.Cast<object?>().Select(_ => ConvertValue(_, elementType)).ToList();
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
        foreach (var item in items)
        {
            list.Add(item);
        }

        if (target.IsArray)
        {
            var array = Array.CreateInstance(elementType, list.Count);
            list.CopyTo(array, 0);
            collection = array;
        }
        else
        {
            collection = list;
        }

        return true;
    }

    static bool TryGetElementType(Type type, out Type elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType()!;
            return true;
        }

        if (type.IsGenericType && type.GetGenericArguments().Length == 1 && typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string)
            && type.GetGenericTypeDefinition() is var definition
            && (definition == typeof(List<>) || definition == typeof(IList<>) || definition == typeof(IReadOnlyList<>) || definition == typeof(IEnumerable<>)
                || definition == typeof(IReadOnlyCollection<>) || definition == typeof(ICollection<>)))
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        elementType = null!;
        return false;
    }
}
