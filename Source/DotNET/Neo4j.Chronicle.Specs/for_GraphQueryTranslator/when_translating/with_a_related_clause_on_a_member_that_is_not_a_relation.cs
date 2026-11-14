// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphQueryTranslator.when_translating;

public class with_a_related_clause_on_a_member_that_is_not_a_relation : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => GraphQueryTranslator.Translate(
        OrganizationViews.Query("PersonWithGroups", GraphQueryShape.List, new GraphRelatedClause("name", [])),
        GraphDatabaseMode.PerNamespace));

    [Fact] void should_refuse_it() => _error.ShouldBeOfExactType<UnknownRelationMember>();
}
