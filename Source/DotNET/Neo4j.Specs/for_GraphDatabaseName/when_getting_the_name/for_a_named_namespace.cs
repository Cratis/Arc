// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.for_GraphDatabaseName.when_getting_the_name;

public class for_a_named_namespace : Specification
{
    string _named;
    string _prefixed;
    string _upperCased;
    string _lowerCasedDefault;

    void Because()
    {
        _named = GraphDatabaseName.For(null, "Orders", "acme");
        _prefixed = GraphDatabaseName.For("cratis", "Orders", "acme");
        _upperCased = GraphDatabaseName.For(null, "Orders", "Acme");
        _lowerCasedDefault = GraphDatabaseName.For(null, "Orders", "default");
    }

    [Fact] void should_join_the_event_store_and_the_namespace() => _named.ShouldEqual("orders-acme");
    [Fact] void should_start_with_the_prefix() => _prefixed.ShouldEqual("cratis-orders-acme");
    [Fact] void should_lower_case_the_namespace() => _upperCased.ShouldEqual("orders-acme");
    [Fact] void should_only_treat_the_exact_default_name_as_the_default_namespace() => _lowerCasedDefault.ShouldEqual("orders-default");
}
