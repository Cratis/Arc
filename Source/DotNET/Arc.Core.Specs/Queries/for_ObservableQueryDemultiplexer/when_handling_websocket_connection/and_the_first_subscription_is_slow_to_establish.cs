// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_websocket_connection;

public class and_the_first_subscription_is_slow_to_establish : given.a_guarded_websocket_connection
{
    TaskCompletionSource<QueryResult> _firstQueryResult;
    bool _secondArrivedBeforeFirstFinished;
    int _performCalls;

    void Establish()
    {
        _queryIds = [FirstQueryId, SecondQueryId];
        _subscriptionRevisions = [1, 1];
        _firstQueryResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        _performCalls = 0;

        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                var result = QueryResult.Success(CorrelationId.New());
                result.Data = _streamingData;

                // The first subscription is held up (a slow query); the second is answered at once.
                return Interlocked.Increment(ref _performCalls) == 1 ? _firstQueryResult.Task : Task.FromResult(result);
            });
    }

    async Task Because() => await RunConnection(async () =>
    {
        _subject.OnNext(["event-store-a"]);

        // If subscriptions were established one at a time, the second could never be answered while the first is held.
        await WaitFor(() => CountQueryResultsFor(SecondQueryId) == 1);
        _secondArrivedBeforeFirstFinished = CountQueryResultsFor(FirstQueryId) == 0;

        var first = QueryResult.Success(CorrelationId.New());
        first.Data = _streamingData;
        _firstQueryResult.SetResult(first);
        await WaitFor(() =>
        {
            // The subject is hot, so keep emitting until the now-released subscription has attached and answers.
            _subject.OnNext(["event-store-b"]);
            return CountQueryResultsFor(FirstQueryId) >= 1;
        });
    });

    [Fact] void should_answer_the_second_subscription_while_the_first_is_still_being_established() => _secondArrivedBeforeFirstFinished.ShouldBeTrue();
    [Fact] void should_still_answer_the_first_subscription() => (CountQueryResultsFor(FirstQueryId) >= 1).ShouldBeTrue();
}
