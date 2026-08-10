// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_guard_allows_an_emission : given.a_guarded_sse_connection
{
    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["event-store-a"]);
        await WaitFor(() => CountQueryResultsFor(FirstQueryId) == 1);
    });

    [Fact] void should_consult_the_guard() => _guardCalls.Count.ShouldEqual(1);
    [Fact] void should_write_the_emission() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);
    [Fact] void should_not_signal_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_return_200_from_subscribe() => _subscribeStatusCodes[FirstQueryId].ShouldEqual(200);
    [Fact] void should_tell_the_guard_the_query_name() => _guardCalls.First().QueryName.Value.ShouldEqual(QueryName);
    [Fact] void should_tell_the_guard_the_caller_identity() => _guardCalls.First().Principal.ShouldEqual(_principal);
}
