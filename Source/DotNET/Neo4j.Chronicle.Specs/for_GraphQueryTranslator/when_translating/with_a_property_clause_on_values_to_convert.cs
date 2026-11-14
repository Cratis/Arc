// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphQueryTranslator.when_translating;

public class with_a_property_clause_on_values_to_convert : Specification
{
    Guid _id;
    GraphCypherQuery _result;

    void Establish() => _id = Guid.NewGuid();

    void Because() => _result = GraphQueryTranslator.Translate(
        OrganizationViews.Query("GroupSummary", GraphQueryShape.List, new GraphPropertyClause("id", "groupId", new GroupId(_id)), new GraphPropertyClause("name", "name", 1.5m)),
        GraphDatabaseMode.PerNamespace);

    [Fact] void should_convert_a_concept_of_a_guid_to_its_string() => _result.Parameters["p0"].ShouldEqual(_id.ToString());
    [Fact] void should_convert_a_decimal_to_a_double() => _result.Parameters["p1"].ShouldEqual(1.5d);
    [Fact] void should_conjoin_the_clauses() => _result.Text.ShouldContain("WHERE n0.`groupId` = $p0 AND n0.`name` = $p1\n");
}
