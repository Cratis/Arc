// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.Chronicle.for_CypherIdentifiers.when_quoting;

public class with_a_plain_identifier : Specification
{
    string _result;

    void Because() => _result = CypherIdentifiers.Quote("MEMBER_OF");

    [Fact] void should_put_it_in_backticks() => _result.ShouldEqual("`MEMBER_OF`");
}
