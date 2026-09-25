// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_unsubscribe;

public class and_caller_does_not_own_the_connection : given.an_sse_connection_owned_by_a_caller
{
    int _resultsAfterTheAttempt;

    async Task Because() => await RunConnection(async () =>
    {
        await _hub.HandleSSESubscribe(SubscribeContextFor(_owner));
        await _hub.HandleSSEUnsubscribe(UnsubscribeContextFor(_anotherCaller));

        _subject.OnNext(["an-item"]);
        await WaitFor(() => CountQueryResults() > 0);
        _resultsAfterTheAttempt = CountQueryResults();
    });

    [Fact] void should_answer_as_though_the_connection_does_not_exist() => _statusCode.ShouldEqual(404);
    [Fact] void should_leave_the_owners_subscription_streaming() => _resultsAfterTheAttempt.ShouldBeGreaterThan(0);
}
