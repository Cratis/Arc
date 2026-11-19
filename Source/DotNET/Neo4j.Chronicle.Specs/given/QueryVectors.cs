// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Graphs;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.Chronicle.given;

/// <summary>
/// Reads the shared graph view query vectors and turns their cases into queries and their results into comparable JSON.
/// </summary>
public static class QueryVectors
{
    static readonly Lazy<JsonObject> _document = new(() =>
        JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "graph-views-query-vectors.json")))!.AsObject());

    /// <summary>
    /// Gets the vectors document.
    /// </summary>
    public static JsonObject Document => _document.Value;

    /// <summary>
    /// Gets the cases.
    /// </summary>
    public static IEnumerable<JsonObject> Cases => Document["cases"]!.AsArray().Select(_ => _!.AsObject());

    /// <summary>
    /// Build the query of a case.
    /// </summary>
    /// <param name="vector">The case.</param>
    /// <returns>The <see cref="GraphQuery"/>.</returns>
    public static GraphQuery QueryOf(JsonObject vector)
    {
        var node = OrganizationViews.Node((string)vector["view"]!);
        var shape = (string)vector["shape"]! == "single" ? GraphQueryShape.First : GraphQueryShape.List;
        return new(OrganizationViews.Graph, shape, node, [.. vector["clauses"]!.AsArray().Select(clause => ClauseOf(node, clause!.AsObject()))]);
    }

    /// <summary>
    /// Get the expected rows of a case: the list, the single row, or none.
    /// </summary>
    /// <param name="vector">The case.</param>
    /// <returns>The expected rows as JSON objects.</returns>
    public static IReadOnlyList<JsonNode> ExpectedRowsOf(JsonObject vector) => vector["expected"] switch
    {
        JsonArray rows => [.. rows.Select(_ => _!)],
        JsonObject row => [row],
        _ => []
    };

    /// <summary>
    /// Turn an expected row into the value the Neo4j driver gives for it: maps, lists, and timestamps as zoned date and times, the
    /// way Chronicle's Neo4j sink stores a <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="node">The JSON of the row.</param>
    /// <param name="name">The member name the value is held by.</param>
    /// <returns>The driver value.</returns>
    public static object? ToDriverValue(JsonNode? node, string? name = null) => node switch
    {
        null => null,
        JsonObject map => map.ToDictionary(_ => _.Key, _ => ToDriverValue(_.Value, _.Key)),
        JsonArray list => list.Select(_ => ToDriverValue(_)).ToList(),
        JsonValue value when name == "since" => new ZonedDateTime(DateTimeOffset.Parse((string)value!, CultureInfo.InvariantCulture)),
        _ => node.GetValue<string>()
    };

    /// <summary>
    /// Turn a mapped row into JSON for comparison.
    /// </summary>
    /// <param name="value">The value of the row or a member.</param>
    /// <returns>The JSON.</returns>
    public static JsonNode? ToJson(object? value) => value switch
    {
        null => null,
        GraphRow row => new JsonObject(row.Values.Select(_ => KeyValuePair.Create(_.Key, ToJson(_.Value)))),
        IEnumerable<GraphRow> rows => new JsonArray([.. rows.Select(ToJson)]),
        string text => JsonValue.Create(text),
        _ => JsonValue.Create(Convert.ToString(value, CultureInfo.InvariantCulture))
    };

    static GraphQueryClause ClauseOf(GraphQueryNode node, JsonObject clause)
    {
        var member = (string)clause["member"]!;
        if ((string)clause["kind"]! == "property")
        {
            return PropertyClauseOf(node, clause);
        }

        var target = node.Members.Single(_ => _.Name == member).Relation!.Target;
        return new GraphRelatedClause(member, [.. clause["clauses"]!.AsArray().Select(nested => PropertyClauseOf(target, nested!.AsObject()))]);
    }

    static GraphPropertyClause PropertyClauseOf(GraphQueryNode node, JsonObject clause)
    {
        var member = (string)clause["member"]!;
        return new(member, node.Members.Single(_ => _.Name == member).Source!, (string?)clause["value"]);
    }
}
