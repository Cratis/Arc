// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_unsubscribe_arrives_before_subscribe : given.a_guarded_sse_connection
{
    int _performCount;

    void Establish() =>
        _queryPipeline.Perform(
                Arg.Any<FullyQualifiedQueryName>(),
                Arg.Any<QueryArguments>(),
                Arg.Any<Paging>(),
                Arg.Any<Sorting>(),
                Arg.Any<IServiceProvider>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                Interlocked.Increment(ref _performCount);
                var result = QueryResult.Success(Cratis.Execution.CorrelationId.New());
                result.Data = _subject;
                return result;
            });

    async Task Because()
    {
        var connectionTask = _hub.HandleSSEConnection(_connectionContext);
        await WaitFor(() => TryExtractConnectionId(out _connectionId));

        try
        {
            await _hub.HandleSSEUnsubscribe(CreateUnsubscribeContext(FirstQueryId, 2));
            await _hub.HandleSSESubscribe(CreateSubscribeContext(FirstQueryId, 1));
            await _hub.HandleSSESubscribe(CreateSubscribeContext(FirstQueryId, 2));
            await _hub.HandleSSESubscribe(CreateSubscribeContext(FirstQueryId, 3));
        }
        finally
        {
            await _connectionCancellation.CancelAsync();
            await connectionTask;
        }
    }

    [Fact] void should_tombstone_equal_and_older_subscribes() => _performCount.ShouldEqual(1);
}
