// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphDriverValues.when_converting;

public class with_a_whole_number : Specification
{
    object? _result;

    void Because() => _result = GraphDriverValues.Convert(42L);

    [Fact] void should_pass_it_through() => ((long)_result!).ShouldEqual(42L);
}
