// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphQueryTranslator.when_translating;

public class with_a_related_clause_without_nested_clauses : Specification
{
    GraphCypherQuery _result;

    void Because() => _result = GraphQueryTranslator.Translate(
        OrganizationViews.Query("PersonWithGroups", GraphQueryShape.List, new GraphRelatedClause("groups", [])),
        GraphDatabaseMode.PerNamespace);

    [Fact] void should_select_the_roots_with_any_related_node() => _result.Text.ShouldContain("WHERE EXISTS { MATCH (n0)-[e1:`MEMBER_OF`]->(n1:`Group`) }\n");
}
