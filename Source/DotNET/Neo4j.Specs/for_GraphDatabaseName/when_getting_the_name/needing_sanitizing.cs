// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.for_GraphDatabaseName.when_getting_the_name;

public class needing_sanitizing : Specification
{
    string _space;
    string _umlaut;
    string _dot;
    string _underscore;

    void Because()
    {
        _space = GraphDatabaseName.For(null, "Orders", "Acme Corp");
        _umlaut = GraphDatabaseName.For(null, "Orders", "Müller");
        _dot = GraphDatabaseName.For(null, "Orders", "acme.eu");
        _underscore = GraphDatabaseName.For(null, "_Orders", "Default");
    }

    [Fact] void should_replace_a_space_and_append_the_hash_of_the_unsanitized_name() => _space.ShouldEqual("orders-acme-corp-fd6ee09c");
    [Fact] void should_replace_a_non_ascii_character_and_append_the_hash() => _umlaut.ShouldEqual("orders-m-ller-21f1b1e6");
    [Fact] void should_replace_a_dot_and_append_the_hash() => _dot.ShouldEqual("orders-acme-eu-550a6c3d");
    [Fact] void should_prefix_a_name_that_starts_with_a_replaced_character_and_append_the_hash() => _underscore.ShouldEqual("g--orders-5cfcdfc7");
}
