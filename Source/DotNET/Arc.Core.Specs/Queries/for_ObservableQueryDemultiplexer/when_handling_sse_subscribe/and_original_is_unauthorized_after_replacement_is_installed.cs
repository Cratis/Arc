// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_original_is_unauthorized_after_replacement_is_installed : given.a_guarded_sse_connection
{
    readonly TaskCompletionSource<QueryResult> _originalResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<QueryResult> _replacementResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly Subject<IEnumerable<string>> _replacementSubject = new();
    int _performCount;

    void Establish()
    {
        _performCount = 0;
        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref _performCount) == 1
                ? _originalResult.Task
                : _replacementResult.Task);
    }

    async Task Because()
    {
        var connectionTask = _hub.HandleSSEConnection(_connectionContext);
        await WaitFor(() => TryExtractConnectionId(out _connectionId));

        try
        {
            var originalSubscribe = _hub.HandleSSESubscribe(CreateSubscribeContext(FirstQueryId, 1));
            await WaitFor(() => Volatile.Read(ref _performCount) == 1);
            var replacementSubscribe = _hub.HandleSSESubscribe(CreateSubscribeContext(FirstQueryId, 2));
            await WaitFor(() => Volatile.Read(ref _performCount) == 2);

            var replacement = QueryResult.Success(CorrelationId.New());
            replacement.Data = _replacementSubject;
            _replacementResult.TrySetResult(replacement);
            await replacementSubscribe;

            _originalResult.TrySetResult(QueryResult.Unauthorized(CorrelationId.New()));
            await originalSubscribe;

            _replacementSubject.OnNext(["current"]);
            await WaitFor(() => CountQueryResultsFor(FirstQueryId) == 1);
        }
        finally
        {
            await _connectionCancellation.CancelAsync();
            await connectionTask;
        }
    }

    [Fact] void should_not_send_stale_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_keep_streaming_the_replacement() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);
}
