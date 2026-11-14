// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphDriverValues.when_converting;

public class with_a_zoned_date_time_in_utc : Specification
{
    object? _result;

    void Because() => _result = GraphDriverValues.Convert(new ZonedDateTime(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));

    [Fact] void should_give_the_iso_8601_text_with_z() => ((string)_result!).ShouldEqual("2026-01-01T00:00:00Z");
}
