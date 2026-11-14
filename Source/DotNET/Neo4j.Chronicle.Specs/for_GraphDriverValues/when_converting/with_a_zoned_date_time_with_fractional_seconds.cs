// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphDriverValues.when_converting;

public class with_a_zoned_date_time_with_fractional_seconds : Specification
{
    object? _result;

    void Because() => _result = GraphDriverValues.Convert(new ZonedDateTime(new DateTimeOffset(2026, 3, 1, 13, 45, 30, 250, TimeSpan.Zero)));

    [Fact] void should_write_the_fraction_without_trailing_zeros() => ((string)_result!).ShouldEqual("2026-03-01T13:45:30.25Z");
}
