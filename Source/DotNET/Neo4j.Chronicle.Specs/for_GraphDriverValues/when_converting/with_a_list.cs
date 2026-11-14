// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphDriverValues.when_converting;

public class with_a_list : Specification
{
    object? _result;

    void Because() => _result = GraphDriverValues.Convert(new List<object> { "a", 1L, new ZonedDateTime(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)) });

    [Fact] void should_convert_every_item() => ((IEnumerable<object?>)_result!).ShouldEqual(new object?[] { "a", 1L, "2026-01-01T00:00:00Z" });
}
