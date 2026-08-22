// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

/// <summary>
/// Subscription B can complete while subscription A for the same query id is still in the query pipeline. A must not
/// install itself over B, send a stale result, or retain its subject observer when it eventually completes.
/// </summary>
public class and_replacement_completes_before_the_original_subscription : given.a_guarded_sse_connection
{
    const string OriginalGeneration = "generation-a";
    const string ReplacementGeneration = "generation-b";

    readonly TaskCompletionSource<QueryResult> _originalResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<QueryResult> _replacementResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly Subject<IEnumerable<string>> _originalSubject = new();
    readonly Subject<IEnumerable<string>> _replacementSubject = new();
    int _performCount;
    bool _originalObserverWasDisposed;

    void Establish()
    {
        _performCount = 0;
        _queryPipeline.Perform(
                Arg.Any<FullyQualifiedQueryName>(),
                Arg.Any<QueryArguments>(),
                Arg.Any<Paging>(),
                Arg.Any<Sorting>(),
                Arg.Any<IServiceProvider>())
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
            var originalSubscribe = _hub.HandleSSESubscribe(CreateSubscribeContext(FirstQueryId, OriginalGeneration));
            await WaitFor(() => Volatile.Read(ref _performCount) == 1);

            var replacementSubscribe = _hub.HandleSSESubscribe(CreateSubscribeContext(FirstQueryId, ReplacementGeneration));
            await WaitFor(() => Volatile.Read(ref _performCount) == 2);

            _replacementResult.TrySetResult(StreamingResult(_replacementSubject));
            await replacementSubscribe;

            _originalResult.TrySetResult(StreamingResult(_originalSubject));
            await originalSubscribe;
            _originalObserverWasDisposed = !_originalSubject.HasObservers;

            _originalSubject.OnNext(["stale"]);
            _replacementSubject.OnNext(["current"]);
            await WaitFor(() => HubMessages.Count(_ => _.Type == ObservableQueryHubMessageType.QueryResult) == 1);
        }
        finally
        {
            await _connectionCancellation.CancelAsync();
            await connectionTask;
        }
    }

    [Fact] void should_dispose_the_stale_created_subscription() => _originalObserverWasDisposed.ShouldBeTrue();
    [Fact] void should_send_only_one_result() => HubMessages.Count(_ => _.Type == ObservableQueryHubMessageType.QueryResult).ShouldEqual(1);
    [Fact] void should_send_only_the_replacement_generation() =>
        typeof(ObservableQueryHubMessage).GetProperty("SubscriptionGeneration")!
            .GetValue(HubMessages.Single(_ => _.Type == ObservableQueryHubMessageType.QueryResult))
            .ShouldEqual(ReplacementGeneration);
    [Fact] void should_not_send_unauthorized() => HasUnauthorizedFor(FirstQueryId).ShouldBeFalse();
    [Fact] void should_not_send_an_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();

    static QueryResult StreamingResult(object data)
    {
        var result = QueryResult.Success(CorrelationId.New());
        result.Data = data;
        return result;
    }
}
