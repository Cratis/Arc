// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Neo4j.Chronicle.for_CypherIdentifiers.when_quoting;

public class with_spaces : Specification
{
    string _result;

    void Because() => _result = CypherIdentifiers.Quote("Line Item");

    [Fact] void should_keep_the_spaces_inside_the_backticks() => _result.ShouldEqual("`Line Item`");
}
