// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_guard_denies_an_emission : given.a_guarded_sse_connection
{
    void Establish() => _verdict = _ => ObservableQueryEmissionVerdict.DenyAndTerminate;

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["event-store-a"]);
        await WaitFor(() => HasUnauthorizedFor(FirstQueryId));

        // Anything the server produces after the denial must never reach the client.
        _subject.OnNext(["event-store-b"]);
        await Task.Delay(50);
    });

    [Fact] void should_not_write_the_emission() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_signal_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_not_surface_an_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_stop_consulting_the_guard() => _guardCalls.Count.ShouldEqual(1);
    [Fact] void should_unregister_the_subscription() => _healthTracker.Received(1).UnregisterSubscription(Arg.Any<string>(), FirstQueryId);
    [Fact] void should_still_have_accepted_the_subscribe() => _subscribeStatusCodes[FirstQueryId].ShouldEqual(200);
}
