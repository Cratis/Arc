// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_subscribes_arrive_out_of_order : given.a_guarded_websocket_connection
{
    int _performCount;

    void Establish()
    {
        _queryIds = [FirstQueryId, FirstQueryId, FirstQueryId];
        _subscriptionRevisions = [2, 1, 2];
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

    [Fact] void should_perform_only_the_newest_revision() => _performCount.ShouldEqual(1);
}
