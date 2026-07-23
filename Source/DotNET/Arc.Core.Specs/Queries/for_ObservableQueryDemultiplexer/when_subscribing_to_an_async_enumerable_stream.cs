// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Execution;

namespace Cratis.Arc.Queries.for_ObservableQueryDemultiplexer;

/// <summary>
/// An IAsyncEnumerable-backed observable query must return a trackable subscription. Returning null left the
/// background stream untracked, so an unsubscribe could not stop it and re-subscribing started a second one.
/// </summary>
public class when_subscribing_to_an_async_enumerable_stream : given.an_observable_query_demultiplexer
{
    TaskCompletionSource _firstItemEmitted;
    TaskCompletionSource _iterationEnded;
    IDisposable _subscription;
    bool _streamStoppedAfterDispose;

    void Establish()
    {
        _firstItemEmitted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _iterationEnded = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    async Task Because()
    {
        _subscription = _hub.SubscribeToStreamingData(
            Stream(),
            "q1",
            new PagingInfo(0, 0, 0),
            null,
            CorrelationId.New(),
            OnNext,
            OnError,
            CancellationToken.None);

        // The stream has emitted its first item and is now parked, so cancelling it exercises a real stop.
        await _firstItemEmitted.Task.WaitAsync(TimeSpan.FromSeconds(2));

        _subscription?.Dispose();

        _streamStoppedAfterDispose = await CompletesWithin(_iterationEnded.Task, TimeSpan.FromSeconds(2));
    }

    [Fact] void should_return_a_trackable_subscription() => _subscription.ShouldNotBeNull();
    [Fact] void should_stop_the_stream_when_the_subscription_is_disposed() => _streamStoppedAfterDispose.ShouldBeTrue();

    Task OnNext(QueryResult result)
    {
        _firstItemEmitted.TrySetResult();
        return Task.CompletedTask;
    }

    Task OnError(string queryId, string message) => Task.CompletedTask;

    async IAsyncEnumerable<int> Stream([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        try
        {
            yield return 1;
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        finally
        {
            _iterationEnded.TrySetResult();
        }
    }

    static async Task<bool> CompletesWithin(Task task, TimeSpan timeout)
    {
        var winner = await Task.WhenAny(task, Task.Delay(timeout));
        return winner == task;
    }
}
