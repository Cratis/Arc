// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphQueryTranslator.when_translating;

public class with_an_identified_edge : Specification
{
    GraphCypherQuery _result;

    void Because() => _result = GraphQueryTranslator.Translate(OrganizationViews.Query("PersonRoles"), GraphDatabaseMode.PerNamespace);

    [Fact] void should_give_one_item_per_edge_ordered_by_far_key_then_identity() => _result.Text.ShouldEqual(
        "MATCH (n0:`Person`)\n" +
        "RETURN {`id`: n0.`personId`, `roles`: COLLECT { MATCH (n0)-[e1:`HAS_ROLE`]->(n1:`Group`) RETURN {`id`: n1.`groupId`, `assignmentId`: e1.`assignmentId`, `role`: e1.`role`} AS item ORDER BY n1.`groupId`, e1.`assignmentId` }} AS row\n" +
        "ORDER BY n0.`personId`");
}
