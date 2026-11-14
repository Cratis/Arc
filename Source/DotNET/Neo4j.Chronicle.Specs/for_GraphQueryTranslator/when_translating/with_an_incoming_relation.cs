// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphQueryTranslator.when_translating;

public class with_an_incoming_relation : Specification
{
    GraphCypherQuery _result;

    void Because() => _result = GraphQueryTranslator.Translate(
        OrganizationViews.Query("GroupWithMembers", GraphQueryShape.List, new GraphRelatedClause("members", [new GraphPropertyClause("name", "name", "Ada")])),
        GraphDatabaseMode.PerNamespace);

    [Fact] void should_follow_the_edge_against_its_direction() => _result.Text.ShouldEqual(
        "MATCH (n0:`Group`)\n" +
        "WHERE EXISTS { MATCH (n0)<-[e1:`MEMBER_OF`]-(n1:`Person`) WHERE n1.`name` = $p0 }\n" +
        "RETURN {`id`: n0.`groupId`, `name`: n0.`name`, `members`: COLLECT { MATCH (n0)<-[e2:`MEMBER_OF`]-(n2:`Person`) RETURN {`id`: n2.`personId`, `name`: n2.`name`, `since`: e2.`since`} AS item ORDER BY n2.`personId` }} AS row\n" +
        "ORDER BY n0.`groupId`");
}
