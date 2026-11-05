// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.for_GraphDatabaseName.when_getting_the_name;

public class for_the_default_namespace : Specification
{
    string _bare;
    string _prefixed;
    string _upperCased;

    void Because()
    {
        _bare = GraphDatabaseName.For(null, "Orders", "Default");
        _prefixed = GraphDatabaseName.For("cratis", "Orders", "Default");
        _upperCased = GraphDatabaseName.For(null, "ORDERS", null);
    }

    [Fact] void should_be_the_lower_cased_event_store() => _bare.ShouldEqual("orders");
    [Fact] void should_start_with_the_prefix() => _prefixed.ShouldEqual("cratis-orders");
    [Fact] void should_not_add_a_hash_for_a_name_that_only_needed_lower_casing() => _upperCased.ShouldEqual("orders");
}
