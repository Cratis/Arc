// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_guard_throws : given.a_guarded_sse_connection
{
    void Establish() => _verdict = _ => throw new TimeoutException("the session store did not answer");

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["event-store-a"]);
        await WaitFor(() => HasUnauthorizedFor(FirstQueryId));

        _subject.OnNext(["event-store-b"]);
        await Task.Delay(50);
    });

    [Fact] void should_not_write_the_emission() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_fail_closed_and_signal_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_not_surface_an_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_stop_consulting_the_guard() => _guardCalls.Count.ShouldEqual(1);
}
