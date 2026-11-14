// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Arc.Neo4j.Testing;
using Cratis.Chronicle.Graphs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Neo4j.Chronicle.for_Neo4jGraphQueryExecutor.when_executing;

public class with_a_query_on_a_database_per_namespace : Specification
{
    GraphSessionSubstitute _session;
    GraphQuery _query;
    IReadOnlyList<GraphRow> _result;

    void Establish()
    {
        _session = new GraphSessionSubstitute { Namespace = "acme" };
        _session.Returns<object>(
            "MATCH (n0:`Group`)",
            new Dictionary<string, object> { ["id"] = "g1", ["name"] = "Engineering" },
            new Dictionary<string, object> { ["id"] = "g2", ["name"] = "Platform" });
        _query = OrganizationViews.Query("GroupSummary", GraphQueryShape.List, new GraphPropertyClause("name", "name", "Engineering"));
    }

    async Task Because() => _result = await new Neo4jGraphQueryExecutor(_session, NullLogger<Neo4jGraphQueryExecutor>.Instance).Execute(_query);

    [Fact] void should_run_the_translated_cypher() => _session.Executions[0].Cypher.ShouldEqual(GraphQueryTranslator.Translate(_query, GraphDatabaseMode.PerNamespace).Text);
    [Fact] void should_give_the_clause_values_as_parameters() => _session.Executions[0].Parameters["p0"].ShouldEqual("Engineering");
    [Fact] void should_give_the_namespace_through_the_session() => _session.Executions[0].Parameters[GraphParameters.Namespace].ShouldEqual("acme");
    [Fact] void should_run_one_query() => _session.Executions.Count.ShouldEqual(1);
    [Fact] void should_map_every_row_in_order() => _result.Select(_ => _["id"]).ShouldEqual(new object?[] { "g1", "g2" });
}
