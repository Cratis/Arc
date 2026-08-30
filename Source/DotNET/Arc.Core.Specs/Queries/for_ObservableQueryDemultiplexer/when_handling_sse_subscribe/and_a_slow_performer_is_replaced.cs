// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_a_slow_performer_is_replaced : given.a_guarded_sse_connection
{
    readonly TaskCompletionSource _performStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _performCancelled = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly Subject<IEnumerable<string>> _replacementSubject = new();
    int _performCount;

    void Establish()
    {
        _queryPipeline.Perform(
                Arg.Any<FullyQualifiedQueryName>(),
                Arg.Any<QueryArguments>(),
                Arg.Any<Paging>(),
                Arg.Any<Sorting>(),
                Arg.Any<IServiceProvider>(),
                Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                if (Interlocked.Increment(ref _performCount) == 1)
                {
                    var token = callInfo.ArgAt<CancellationToken>(5);
                    _performStarted.TrySetResult();
                    try
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        _performCancelled.TrySetResult();
                        throw;
                    }
                }

                var result = QueryResult.Success(CorrelationId.New());
                result.Data = _replacementSubject;
                return result;
            });
    }

    async Task Because()
    {
        var connectionTask = _hub.HandleSSEConnection(_connectionContext);
        await WaitFor(() => TryExtractConnectionId(out _connectionId));

        try
        {
            var original = _hub.HandleSSESubscribe(CreateSubscribeContext(FirstQueryId, 1));
            await _performStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await _hub.HandleSSESubscribe(CreateSubscribeContext(FirstQueryId, 2));
            await _performCancelled.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await original;
        }
        finally
        {
            await _connectionCancellation.CancelAsync();
            await connectionTask;
        }
    }

    [Fact] void should_cancel_the_slow_query_performer() => _performCancelled.Task.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_run_the_replacement() => _performCount.ShouldEqual(2);
}
