// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.for_GraphDatabaseName.when_getting_the_name;

public class without_an_event_store : Specification
{
    Exception _result;

    void Because() => _result = Catch.Exception(() => GraphDatabaseName.For(null, string.Empty, "acme"));

    [Fact] void should_refuse() => _result.ShouldBeOfExactType<ArgumentException>();
}
