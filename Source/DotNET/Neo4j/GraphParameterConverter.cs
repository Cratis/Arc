// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Reflection;
using Cratis.Concepts;

namespace Cratis.Arc.Neo4j;

/// <summary>
/// Converts the parameters of a Cypher query into values the Neo4j driver can send.
/// </summary>
/// <remarks>
/// A <see cref="ConceptAs{T}"/> binds as its primitive value, so a query can be given <c>new { personId }</c> with a
/// <c>PersonId</c> concept. The conversion applies to nested lists and maps too. A <see cref="Guid"/> binds as its
/// string form, an enum as its name and a <see cref="decimal"/> as a <see cref="double"/>, because the graph has no
/// type for them. Everything else is passed on untouched.
/// </remarks>
public static class GraphParameterConverter
{
    /// <summary>
    /// Convert the parameters of a query.
    /// </summary>
    /// <param name="parameters">Null, an <see cref="IDictionary{TKey,TValue}"/> of string keys, or an object whose public properties are the parameters.</param>
    /// <returns>The converted parameters, ready for the driver.</returns>
    public static IDictionary<string, object?> Convert(object? parameters)
    {
        var result = new Dictionary<string, object?>();
        switch (parameters)
        {
            case null:
                break;
            case IEnumerable<KeyValuePair<string, object?>> pairs:
                foreach (var pair in pairs)
                {
                    result[pair.Key] = ConvertValue(pair.Value);
                }

                break;
            case IDictionary dictionary:
                foreach (DictionaryEntry entry in dictionary)
                {
                    result[entry.Key.ToString()!] = ConvertValue(entry.Value);
                }

                break;
            default:
                foreach (var property in parameters.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(_ => _.GetIndexParameters().Length == 0))
                {
                    result[property.Name] = ConvertValue(property.GetValue(parameters));
                }

                break;
        }

        return result;
    }

    /// <summary>
    /// Convert a single parameter value.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The value the driver can send.</returns>
    public static object? ConvertValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var type = value.GetType();
        if (type.IsConcept())
        {
            return ConvertValue(value.GetConceptValue());
        }

        return value switch
        {
            Guid guid => guid.ToString(),
            Enum enumValue => enumValue.ToString(),
            decimal number => (double)number,
            string => value,
            IDictionary dictionary => ConvertDictionary(dictionary),
            IEnumerable enumerable => enumerable.Cast<object?>().Select(ConvertValue).ToList(),
            _ => value
        };
    }

    static Dictionary<string, object?> ConvertDictionary(IDictionary dictionary)
    {
        var result = new Dictionary<string, object?>();
        foreach (DictionaryEntry entry in dictionary)
        {
            result[entry.Key.ToString()!] = ConvertValue(entry.Value);
        }

        return result;
    }
}
