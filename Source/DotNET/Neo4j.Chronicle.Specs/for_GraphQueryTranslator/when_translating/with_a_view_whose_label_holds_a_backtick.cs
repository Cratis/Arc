// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Neo4j.Chronicle.given;
using Cratis.Chronicle.Graphs;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphQueryTranslator.when_translating;

public class with_a_view_whose_label_holds_a_backtick : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => GraphQueryTranslator.Translate(
        new GraphQuery("Organization", GraphQueryShape.List, new GraphQueryNode("Bad", "Person`) DETACH DELETE (x", "personId", []), []),
        GraphDatabaseMode.PerNamespace));

    [Fact] void should_refuse_the_identifier() => _error.ShouldBeOfExactType<InvalidCypherIdentifier>();
}
