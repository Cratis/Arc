// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.Filters.for_AvailabilityFilter;

public class when_the_query_carries_no_gate : given.a_query_filter_with_a_gate
{
    void Establish() => _performer.Type.Returns(typeof(Ungated));

    async Task Because() => _result = await _filter.OnPerform(_context);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_decide() => _gate.DidNotReceiveWithAnyArgs().Decide(default!, default!, Arg.Any<CancellationToken>());

    public record Ungated;
}
