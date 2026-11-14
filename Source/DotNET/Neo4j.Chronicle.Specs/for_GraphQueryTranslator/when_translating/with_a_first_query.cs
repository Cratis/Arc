// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphQueryTranslator.when_translating;

public class with_a_first_query : Specification
{
    GraphCypherQuery _result;

    void Because() => _result = GraphQueryTranslator.Translate(OrganizationViews.Query("GroupSummary", GraphQueryShape.First), GraphDatabaseMode.PerNamespace);

    [Fact] void should_take_the_first_root_in_key_order() => _result.Text.ShouldEqual(
        "MATCH (n0:`Group`)\n" +
        "RETURN {`id`: n0.`groupId`, `name`: n0.`name`} AS row\n" +
        "ORDER BY n0.`groupId`\n" +
        "LIMIT 1");
}
