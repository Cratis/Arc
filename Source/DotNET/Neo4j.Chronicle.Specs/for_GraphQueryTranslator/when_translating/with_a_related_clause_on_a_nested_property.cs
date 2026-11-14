// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphQueryTranslator.when_translating;

public class with_a_related_clause_on_a_nested_property : Specification
{
    GraphCypherQuery _result;

    void Because() => _result = GraphQueryTranslator.Translate(
        OrganizationViews.Query("PersonWithGroups", GraphQueryShape.List, new GraphRelatedClause("groups", [new GraphPropertyClause("id", "groupId", "00000000-0000-0000-0000-000000000101")])),
        GraphDatabaseMode.PerNamespace);

    [Fact] void should_select_the_roots_with_an_exists_subquery_and_still_hydrate_the_full_relation() => _result.Text.ShouldEqual(
        "MATCH (n0:`Person`)\n" +
        "WHERE EXISTS { MATCH (n0)-[e1:`MEMBER_OF`]->(n1:`Group`) WHERE n1.`groupId` = $p0 }\n" +
        "RETURN {`id`: n0.`personId`, `name`: n0.`name`, `groups`: COLLECT { MATCH (n0)-[e2:`MEMBER_OF`]->(n2:`Group`) RETURN {`id`: n2.`groupId`, `name`: n2.`name`, `since`: e2.`since`} AS item ORDER BY n2.`groupId` }} AS row\n" +
        "ORDER BY n0.`personId`");
    [Fact] void should_give_the_nested_value_as_a_parameter() => _result.Parameters["p0"].ShouldEqual("00000000-0000-0000-0000-000000000101");
}
