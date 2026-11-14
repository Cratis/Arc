// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphQueryTranslator.when_translating;

public class in_single_database_mode : Specification
{
    GraphCypherQuery _result;

    void Because() => _result = GraphQueryTranslator.Translate(
        OrganizationViews.Query("PersonWithGroups", GraphQueryShape.List, new GraphPropertyClause("name", "name", "Ada"), new GraphRelatedClause("groups", [new GraphPropertyClause("name", "name", "Engineering")])),
        GraphDatabaseMode.Single);

    [Fact] void should_constrain_every_node_and_edge_of_every_pattern_to_the_namespace() => _result.Text.ShouldEqual(
        "MATCH (n0:`Person`)\n" +
        "WHERE n0.`_namespace` = $namespace AND n0.`name` = $p0 AND EXISTS { MATCH (n0)-[e1:`MEMBER_OF`]->(n1:`Group`) WHERE e1.`_namespace` = $namespace AND n1.`_namespace` = $namespace AND n1.`name` = $p1 }\n" +
        "RETURN {`id`: n0.`personId`, `name`: n0.`name`, `groups`: COLLECT { MATCH (n0)-[e2:`MEMBER_OF`]->(n2:`Group`) WHERE e2.`_namespace` = $namespace AND n2.`_namespace` = $namespace RETURN {`id`: n2.`groupId`, `name`: n2.`name`, `since`: e2.`since`} AS item ORDER BY n2.`groupId` }} AS row\n" +
        "ORDER BY n0.`personId`");
    [Fact] void should_leave_the_namespace_parameter_to_the_session() => _result.Parameters.Keys.ShouldContainOnly("p0", "p1");
}
