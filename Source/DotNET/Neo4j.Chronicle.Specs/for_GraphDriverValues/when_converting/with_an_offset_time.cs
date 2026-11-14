// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Neo4j.Driver;

namespace Cratis.Arc.Neo4j.Chronicle.for_GraphDriverValues.when_converting;

public class with_an_offset_time : Specification
{
    object? _result;

    void Because() => _result = GraphDriverValues.Convert(new OffsetTime(13, 45, 30, 0, 3600));

    [Fact] void should_give_the_iso_8601_time_with_the_offset() => ((string)_result!).ShouldEqual("13:45:30+01:00");
}
