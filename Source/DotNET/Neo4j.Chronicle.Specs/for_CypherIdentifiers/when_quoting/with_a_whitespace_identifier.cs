// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.Chronicle.for_CypherIdentifiers.when_quoting;

public class with_a_whitespace_identifier : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => CypherIdentifiers.Quote("  "));

    [Fact] void should_refuse_it() => _error.ShouldBeOfExactType<InvalidCypherIdentifier>();
}
