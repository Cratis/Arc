// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle;

/// <summary>
/// Maps the rows the translation of a graph view query returns into the neutral <see cref="GraphRow"/> tree the Chronicle
/// hydrator builds views from.
/// </summary>
/// <remarks>
/// <para>
/// A row is a map keyed by member name. A property or an edge property is converted with <see cref="GraphDriverValues"/>;
/// a relation of cardinality many becomes a list of rows, empty when the map holds no items, and a relation of cardinality
/// one becomes a row or null. A member the map does not hold reads as null.
/// </para>
/// </remarks>
public static class GraphRowMapper
{
    /// <summary>
    /// Map a row.
    /// </summary>
    /// <param name="value">The value of the row column, as the driver returned it.</param>
    /// <param name="node">The <see cref="GraphQueryNode"/> the row was read for.</param>
    /// <returns>The <see cref="GraphRow"/>.</returns>
    /// <exception cref="UnexpectedGraphRow">Thrown when the value is not a map.</exception>
    public static GraphRow Map(object? value, GraphQueryNode node)
    {
        var map = MapOf(value) ?? throw new UnexpectedGraphRow(node.View, value);
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var member in node.Members)
        {
            var memberValue = map.TryGetValue(member.Name, out var found) ? found : null;
            values[member.Name] = member.Relation is { } relation ? Related(memberValue, relation) : GraphDriverValues.Convert(memberValue);
        }

        return new(values);
    }

    static object? Related(object? value, GraphQueryRelation relation)
    {
        if (relation.Cardinality == GraphViewCardinality.One)
        {
            return value is null ? null : Map(value, relation.Target);
        }

        return value is IEnumerable list and not string
            ? list.Cast<object?>().Select(item => Map(item, relation.Target)).ToList()
            : new List<GraphRow>();
    }

    static IReadOnlyDictionary<string, object?>? MapOf(object? value) => value switch
    {
        IReadOnlyDictionary<string, object?> map => map,
        IDictionary dictionary => dictionary.Keys.Cast<object>().ToDictionary(_ => _.ToString()!, _ => dictionary[_], StringComparer.Ordinal),
        _ => null
    };
}
