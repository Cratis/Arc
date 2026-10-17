// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_guard_suppresses_a_snapshot : given.a_guarded_websocket_connection
{
    void Establish()
    {
        _streamingData = new[] { "withheld" };
        _verdict = _ => ObservableQueryEmissionVerdict.Suppress;
    }

    async Task Because() => await RunConnection(() => Task.CompletedTask);

    [Fact] void should_not_send_data() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_not_signal_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_not_signal_an_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_consult_the_guard_once() => _guardCalls.Count.ShouldEqual(1);
}
