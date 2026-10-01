// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Observability.for_CardinalityLimiter;

public class when_many_threads_record_distinct_values : Specification
{
    const int Limit = 100;
    const int Values = 10_000;

    CardinalityLimiter _limiter;
    string[] _recorded;

    void Establish()
    {
        _limiter = new(Limit);
        _recorded = new string[Values];
    }

    void Because() => Parallel.For(0, Values, index => _recorded[index] = _limiter.Limit($"Command{index}"));

    [Fact] void should_let_exactly_the_limit_through() => _recorded.Count(_ => _ != WellKnownTelemetryNames.Other).ShouldEqual(Limit);
    [Fact] void should_count_exactly_the_limit() => _limiter.Count.ShouldEqual(Limit);
}
