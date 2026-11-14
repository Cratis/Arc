// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphQueryTranslator.when_translating;

public class with_an_edge_property_on_the_root_view : Specification
{
    GraphCypherQuery _result;

    void Because() => _result = GraphQueryTranslator.Translate(OrganizationViews.Query("GroupMembership"), GraphDatabaseMode.PerNamespace);

    [Fact] void should_read_the_edge_property_as_null() => _result.Text.ShouldContain("`since`: null}");
}
