// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphQueryTranslator.when_translating;

public class with_a_property_clause : Specification
{
    GraphCypherQuery _result;

    void Because() => _result = GraphQueryTranslator.Translate(
        OrganizationViews.Query("GroupSummary", GraphQueryShape.List, new GraphPropertyClause("name", "name", "Engineering")),
        GraphDatabaseMode.PerNamespace);

    [Fact] void should_select_the_roots_by_a_parameter() => _result.Text.ShouldEqual(
        "MATCH (n0:`Group`)\n" +
        "WHERE n0.`name` = $p0\n" +
        "RETURN {`id`: n0.`groupId`, `name`: n0.`name`} AS row\n" +
        "ORDER BY n0.`groupId`");
    [Fact] void should_give_the_value_as_a_parameter() => _result.Parameters["p0"].ShouldEqual("Engineering");
    [Fact] void should_give_only_that_parameter() => _result.Parameters.Count.ShouldEqual(1);
}
