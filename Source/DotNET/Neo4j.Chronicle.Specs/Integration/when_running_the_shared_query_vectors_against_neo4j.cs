// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Neo4j.Chronicle.given;
using Microsoft.Extensions.Logging.Abstractions;
using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.Chronicle.Integration;

/// <summary>
/// Seeds the graph of the shared query vectors into a live Neo4j, in its own namespace in single database mode, and runs every
/// case through the executor, comparing the rows with the expected result.
/// </summary>
public class when_running_the_shared_query_vectors_against_neo4j : Specification
{
    readonly List<string> _mismatches = [];
    readonly string _namespace = $"specs-{Guid.NewGuid():N}";
    IDriver? _driver;
    int _cases;

    async Task Because()
    {
        if (!LiveNeo4j.Enabled) return;

        _driver = LiveNeo4j.CreateDriver();
        await using (var writer = Session(AccessMode.Write))
        {
            await Seed(writer);
        }

        await using var reader = Session(AccessMode.Read);
        var executor = new Neo4jGraphQueryExecutor(reader, NullLogger<Neo4jGraphQueryExecutor>.Instance);
        foreach (var vector in QueryVectors.Cases)
        {
            _cases++;
            var rows = await executor.Execute(QueryVectors.QueryOf(vector));
            var expected = QueryVectors.ExpectedRowsOf(vector);
            if (!JsonNode.DeepEquals(new JsonArray([.. rows.Select(QueryVectors.ToJson)]), new JsonArray([.. expected.Select(_ => _.DeepClone())])))
            {
                _mismatches.Add($"{vector["name"]}: {new JsonArray([.. rows.Select(QueryVectors.ToJson)]).ToJsonString()}");
            }
        }
    }

    async Task Destroy()
    {
        if (_driver is null) return;
        await using (var writer = Session(AccessMode.Write))
        {
            await Execute(writer, $"MATCH (n) WHERE n.`{GraphParameters.NamespaceProperty}` = $namespace DETACH DELETE n");
        }

        await _driver.DisposeAsync();
    }

    [LiveNeo4jFact] void should_run_every_case() => _cases.ShouldEqual(9);
    [LiveNeo4jFact] void should_give_the_expected_result_for_every_case() => _mismatches.ShouldBeEmpty();

    static async Task Seed(GraphSession writer)
    {
        var keys = new Dictionary<string, string> { ["Person"] = "personId", ["Group"] = "groupId" };
        foreach (var node in QueryVectors.Document["nodes"]!.AsArray())
        {
            await Execute(
                writer,
                $"CREATE (n:{CypherIdentifiers.Quote((string)node!["label"]!)}) SET n = $properties, n.`{GraphParameters.NamespaceProperty}` = $namespace",
                new Dictionary<string, object?> { ["properties"] = QueryVectors.ToDriverValue(node["properties"], "properties") });
        }

        foreach (var edge in QueryVectors.Document["edges"]!.AsArray())
        {
            var (source, target) = ((string)edge!["source"]!["label"]!, (string)edge["target"]!["label"]!);
            var cypher =
                $"MATCH (s:{CypherIdentifiers.Quote(source)} {{{CypherIdentifiers.Quote(keys[source])}: $sourceKey, `{GraphParameters.NamespaceProperty}`: $namespace}}) " +
                $"MATCH (t:{CypherIdentifiers.Quote(target)} {{{CypherIdentifiers.Quote(keys[target])}: $targetKey, `{GraphParameters.NamespaceProperty}`: $namespace}}) " +
                $"CREATE (s)-[r:{CypherIdentifiers.Quote((string)edge["type"]!)}]->(t) SET r = $properties, r.`{GraphParameters.NamespaceProperty}` = $namespace";
            await Execute(
                writer,
                cypher,
                new Dictionary<string, object?>
                {
                    ["sourceKey"] = (string)edge["source"]!["key"]!,
                    ["targetKey"] = (string)edge["target"]!["key"]!,
                    ["properties"] = QueryVectors.ToDriverValue(edge["properties"], "properties")
                });
        }
    }

    static async Task Execute(GraphSession session, string cypher, object? parameters = null) =>
        await (await session.Run(cypher, parameters)).ConsumeAsync();

    GraphSession Session(AccessMode accessMode) => new(new GraphConnection(_driver!, LiveNeo4j.Database), _namespace, GraphDatabaseMode.Single, accessMode);
}
