// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_no_guard_is_registered_for_a_snapshot : given.a_guarded_sse_connection
{
    void Establish()
    {
        _streamingData = new[] { "snapshot" };
        UseGuards(Substitute.For<IObservableQueryEmissionGuards>());
    }

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_send_the_snapshot() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);
    [Fact] void should_not_construct_a_guard_context() => _guardCalls.ShouldBeEmpty();
    [Fact] void should_not_signal_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
}
