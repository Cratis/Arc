// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_unsubscribe_arrives_before_subscribe : given.a_guarded_websocket_connection
{
    int _performCount;

    void Establish()
    {
        _unsubscribeBeforeSubscribeRevision = 2;
        _queryIds = [FirstQueryId, FirstQueryId, FirstQueryId];
        _subscriptionRevisions = [1, 2, 3];
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
    }

    Task Because() => RunConnection(() => Task.CompletedTask);

    [Fact] void should_tombstone_equal_and_older_subscribes() => _performCount.ShouldEqual(1);
}
