// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.for_GraphDatabaseName.when_getting_the_name;

public class not_starting_with_a_letter : Specification
{
    string _result;

    void Because() => _result = GraphDatabaseName.For(null, "1Orders", "Default");

    [Fact] void should_be_prefixed_with_g() => _result.ShouldEqual("g-1orders");
}
