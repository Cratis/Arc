// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_guard_denies_one_of_two_subscriptions : given.a_guarded_sse_connection
{
    CorrelationId _deniedSubscription;

    void Establish()
    {
        _queryIds = [FirstQueryId, SecondQueryId];
        _verdict = context => context.CorrelationId == _deniedSubscription
            ? ObservableQueryEmissionVerdict.DenyAndTerminate
            : ObservableQueryEmissionVerdict.Allow;
    }

    async Task Because() => await RunConnection(async () =>
    {
        _deniedSubscription = _correlationIds.First();

        _subject.OnNext(["event-store-a"]);
        await WaitFor(() => HasUnauthorizedFor(FirstQueryId) && CountQueryResultsFor(SecondQueryId) == 1);

        _subject.OnNext(["event-store-b"]);
        await WaitFor(() => CountQueryResultsFor(SecondQueryId) == 2);
    });

    [Fact] void should_signal_unauthorized_for_the_denied_subscription() => HasUnauthorizedFor(FirstQueryId).ShouldBeTrue();
    [Fact] void should_write_nothing_for_the_denied_subscription() => CountQueryResultsFor(FirstQueryId).ShouldEqual(0);
    [Fact] void should_not_signal_unauthorized_for_the_sibling() => HasUnauthorizedFor(SecondQueryId).ShouldBeFalse();
    [Fact] void should_keep_streaming_the_sibling() => CountQueryResultsFor(SecondQueryId).ShouldEqual(2);
}
