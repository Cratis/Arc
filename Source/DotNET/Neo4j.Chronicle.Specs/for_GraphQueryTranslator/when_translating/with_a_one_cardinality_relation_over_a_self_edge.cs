// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphQueryTranslator.when_translating;

public class with_a_one_cardinality_relation_over_a_self_edge : Specification
{
    GraphCypherQuery _result;

    void Because() => _result = GraphQueryTranslator.Translate(OrganizationViews.Query("GroupWithParent"), GraphDatabaseMode.PerNamespace);

    [Fact] void should_take_the_first_related_item_or_null() => _result.Text.ShouldEqual(
        "MATCH (n0:`Group`)\n" +
        "RETURN {`id`: n0.`groupId`, `name`: n0.`name`, `parent`: head(COLLECT { MATCH (n0)-[e1:`PART_OF`]->(n1:`Group`) RETURN {`id`: n1.`groupId`, `name`: n1.`name`} AS item ORDER BY n1.`groupId` })} AS row\n" +
        "ORDER BY n0.`groupId`");
}
