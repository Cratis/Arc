// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphQueryTranslator.when_translating;

public class with_a_list_query : Specification
{
    GraphCypherQuery _result;

    void Because() => _result = GraphQueryTranslator.Translate(OrganizationViews.Query("PersonWithGroups"), GraphDatabaseMode.PerNamespace);

    [Fact] void should_match_the_roots_return_one_map_per_root_and_hydrate_the_full_relation_ordered_by_key() => _result.Text.ShouldEqual(
        "MATCH (n0:`Person`)\n" +
        "RETURN {`id`: n0.`personId`, `name`: n0.`name`, `groups`: COLLECT { MATCH (n0)-[e1:`MEMBER_OF`]->(n1:`Group`) RETURN {`id`: n1.`groupId`, `name`: n1.`name`, `since`: e1.`since`} AS item ORDER BY n1.`groupId` }} AS row\n" +
        "ORDER BY n0.`personId`");
    [Fact] void should_have_no_parameters() => _result.Parameters.ShouldBeEmpty();
}
