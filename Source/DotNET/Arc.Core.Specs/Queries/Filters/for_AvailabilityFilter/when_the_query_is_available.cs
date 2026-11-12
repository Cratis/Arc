// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability;

namespace Cratis.Arc.Queries.Filters.for_AvailabilityFilter;

public class when_the_query_is_available : given.a_query_filter_with_a_gate
{
    void Establish() =>
        _gate.Decide(typeof(given.a_query_filter_with_a_gate.GatedReadModel), _services, Arg.Any<CancellationToken>()).Returns(AvailabilityDecision.Available);

    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
}
