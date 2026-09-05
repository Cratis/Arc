// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Cratis.Execution;
using NSubstitute.Exceptions;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer.when_handling_sse_subscribe;

public class and_replaced_async_enumerable_completes : given.a_guarded_sse_connection
{
    const long OriginalRevision = 1;
    const long ReplacementRevision = 2;

    readonly TaskCompletionSource _originalRelease = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _originalStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _originalExited = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly Subject<IEnumerable<string>> _replacementSubject = new();
    int _performCount;
    bool _replacementHealthSurvivedOriginalCompletion;
    bool _replacementHadObservers;

    void Establish()
    {
        _performCount = 0;
        _queryPipeline.Perform(Arg.Any<FullyQualifiedQueryName>(), Arg.Any<QueryArguments>(), Arg.Any<Paging>(), Arg.Any<Sorting>(), Arg.Any<IServiceProvider>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(StreamingResult(
                Interlocked.Increment(ref _performCount) == 1
                    ? OriginalStream()
                    : _replacementSubject)));
    }

    async Task Because()
    {
        var connectionTask = _hub.HandleSSEConnection(_connectionContext);
        await WaitFor(() => TryExtractConnectionId(out _connectionId));

        try
        {
            await _hub.HandleSSESubscribe(CreateSubscribeContext(FirstQueryId, OriginalRevision));
            await _originalStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));

            await _hub.HandleSSESubscribe(CreateSubscribeContext(FirstQueryId, ReplacementRevision));
            _originalRelease.TrySetResult();
            await _originalExited.Task.WaitAsync(TimeSpan.FromSeconds(2));
            await Task.Delay(50);

            try
            {
                _healthTracker.Received(1).UnregisterSubscription(Arg.Any<string>(), FirstQueryId);
                _replacementHealthSurvivedOriginalCompletion = true;
            }
            catch (ReceivedCallsException)
            {
                _replacementHealthSurvivedOriginalCompletion = false;
            }

            _replacementHadObservers = _replacementSubject.HasObservers;
            _replacementSubject.OnNext(["current"]);
            await WaitFor(() => CountQueryResultsFor(FirstQueryId) == 1);
        }
        finally
        {
            await _connectionCancellation.CancelAsync();
            await connectionTask;
        }
    }

    [Fact] void should_not_let_the_old_completion_unregister_replacement_health() =>
        _replacementHealthSurvivedOriginalCompletion.ShouldBeTrue();
    [Fact] void should_keep_the_replacement_observer_attached() => _replacementHadObservers.ShouldBeTrue();
    [Fact] void should_send_only_the_replacement_result() => CountQueryResultsFor(FirstQueryId).ShouldEqual(1);
    [Fact] void should_send_only_the_replacement_revision() =>
        typeof(ObservableQueryHubMessage).GetProperty("Revision")!
            .GetValue(HubMessages.Single(_ => _.Type == ObservableQueryHubMessageType.QueryResult))
            .ShouldEqual(ReplacementRevision);
    [Fact] void should_unregister_each_real_subscription_once() =>
        _healthTracker.Received(2).UnregisterSubscription(Arg.Any<string>(), FirstQueryId);
    [Fact] void should_not_send_an_error() => HasErrorFor(FirstQueryId).ShouldBeFalse();

    async IAsyncEnumerable<IEnumerable<string>> OriginalStream([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _originalStarted.TrySetResult();
        try
        {
            await _originalRelease.Task;
        }
        finally
        {
            _originalExited.TrySetResult();
        }

        yield break;
    }

    static QueryResult StreamingResult(object data)
    {
        var result = QueryResult.Success(CorrelationId.New());
        result.Data = data;
        return result;
    }
}
