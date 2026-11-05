// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.for_GraphDatabaseName.when_getting_the_name;

public class shorter_than_the_shortest_name : Specification
{
    string _result;

    void Because() => _result = GraphDatabaseName.For(null, "ab", "Default");

    [Fact] void should_append_the_hash_to_reach_the_shortest_length() => _result.ShouldEqual("ab-fb8e20fc");
}
