// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphQueryTranslator.when_translating;

public class with_a_property_clause_for_null : Specification
{
    GraphCypherQuery _result;

    void Because() => _result = GraphQueryTranslator.Translate(
        OrganizationViews.Query("GroupSummary", GraphQueryShape.List, new GraphPropertyClause("name", "name", null)),
        GraphDatabaseMode.PerNamespace);

    [Fact] void should_select_the_roots_without_the_property() => _result.Text.ShouldContain("WHERE n0.`name` IS NULL\n");
    [Fact] void should_have_no_parameters() => _result.Parameters.ShouldBeEmpty();
}
