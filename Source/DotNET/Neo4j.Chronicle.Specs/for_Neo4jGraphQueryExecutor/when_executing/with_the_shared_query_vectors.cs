// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Arc.Neo4j.Testing;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Neo4j.Chronicle.for_Neo4jGraphQueryExecutor.when_executing;

/// <summary>
/// Runs every case of the shared query vectors through the executor on a substitute session.
/// </summary>
/// <remarks>
/// The substitute does not evaluate Cypher. It answers the query of each case with the rows the case expects, in the shape the
/// Neo4j driver gives them (maps, lists and zoned date and times), so this proves the executor runs exactly the translated
/// query with its parameters and maps the driver's rows to the expected rows. That the Cypher selects those rows is proven
/// only against a real Neo4j, by the opt-in integration specification.
/// </remarks>
public class with_the_shared_query_vectors : Specification
{
    readonly List<string> _mismatches = [];
    readonly List<string> _wrongQueries = [];
    int _cases;

    async Task Because()
    {
        foreach (var vector in QueryVectors.Cases)
        {
            _cases++;
            var name = (string)vector["name"]!;
            var query = QueryVectors.QueryOf(vector);
            var expected = QueryVectors.ExpectedRowsOf(vector);
            var cypher = GraphQueryTranslator.Translate(query, GraphDatabaseMode.Single);

            var session = new GraphSessionSubstitute { Namespace = "vectors", DatabaseMode = GraphDatabaseMode.Single };
            session.Returns<object>(cypher.Text, [.. expected.Select(row => QueryVectors.ToDriverValue(row)!)]);
            var rows = await new Neo4jGraphQueryExecutor(session, NullLogger<Neo4jGraphQueryExecutor>.Instance).Execute(query);

            var execution = session.Executions.Single();
            if (execution.Cypher != cypher.Text || cypher.Parameters.Any(_ => !Equals(execution.Parameters[_.Key], _.Value)) || (string?)execution.Parameters[GraphParameters.Namespace] != "vectors")
            {
                _wrongQueries.Add(name);
            }

            if (!JsonNode.DeepEquals(new JsonArray([.. rows.Select(QueryVectors.ToJson)]), new JsonArray([.. expected.Select(_ => _.DeepClone())])))
            {
                _mismatches.Add(name);
            }
        }
    }

    [Fact] void should_run_every_case() => _cases.ShouldEqual(9);
    [Fact] void should_run_the_translated_query_with_its_parameters_and_the_namespace_for_every_case() => _wrongQueries.ShouldBeEmpty();
    [Fact] void should_map_the_driver_rows_to_the_expected_result_for_every_case() => _mismatches.ShouldBeEmpty();
}
