// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_unsubscribe;

public class and_old_unsubscribe_arrives_after_replacement : given.a_guarded_sse_connection
{
    async Task Because()
    {
        var connectionTask = _hub.HandleSSEConnection(_connectionContext);
        await WaitFor(() => TryExtractConnectionId(out _connectionId));

        try
        {
            await _hub.HandleSSESubscribe(CreateSubscribeContext(FirstQueryId, 1));
            await _hub.HandleSSESubscribe(CreateSubscribeContext(FirstQueryId, 2));
            await _hub.HandleSSEUnsubscribe(CreateUnsubscribeContext(FirstQueryId, 1));
            _subject.OnNext(["current"]);
            await WaitFor(() => CountQueryResultsFor(FirstQueryId) == 1);
        }
        finally
        {
            await _connectionCancellation.CancelAsync();
            await connectionTask;
        }
    }

    [Fact] void should_keep_the_replacement_streaming() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);
}
