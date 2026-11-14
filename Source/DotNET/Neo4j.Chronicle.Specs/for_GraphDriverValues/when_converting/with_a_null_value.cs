// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphDriverValues.when_converting;

public class with_a_null_value : Specification
{
    object? _result;

    void Because() => _result = GraphDriverValues.Convert(null);

    [Fact] void should_give_null() => _result.ShouldBeNull();
}
